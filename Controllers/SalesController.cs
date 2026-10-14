using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;
using Pharmacy.Models.ViewModels;

namespace Pharmacy.Controllers;

[Authorize(Roles = RoleNames.AdminOrPharmacist)]
public class SalesController : Controller
{
    private const decimal MaxSaleAmount = 9_999_999_999.99m;
    private readonly ApplicationDbContext _context;

    public SalesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateSaleViewModel();
        await LoadAvailableMedicinesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSaleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadAvailableMedicinesAsync(model);
            return View(model);
        }

        // Повторяющиеся строки одного товара объединяются.
        var requestedItems = model.Items
            .GroupBy(item => item.MedicineId)
            .Select(group => new
            {
                MedicineId = group.Key,
                Quantity = group.Sum(item => (long)item.Quantity)
            })
            .ToList();

        if (requestedItems.Any(item => item.Quantity > int.MaxValue))
        {
            ModelState.AddModelError(string.Empty, "Суммарное количество товара слишком велико.");
            await LoadAvailableMedicinesAsync(model);
            return View(model);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var medicineIds = requestedItems.Select(item => item.MedicineId).ToArray();
        var medicines = await _context.Medicines
            .AsNoTracking()
            .Where(medicine =>
                medicineIds.Contains(medicine.Id) &&
                medicine.IsActive &&
                medicine.ExpirationDate >= today)
            .ToDictionaryAsync(medicine => medicine.Id);

        if (medicines.Count != requestedItems.Count)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(
                string.Empty,
                "Один из товаров недоступен для продажи или просрочен.");
            await LoadAvailableMedicinesAsync(model);
            return View(model);
        }

        var saleItems = new List<SaleItem>();
        decimal totalAmount = 0;

        foreach (var requested in requestedItems)
        {
            var medicine = medicines[requested.MedicineId];
            var quantity = (int)requested.Quantity;
            var lineTotal = medicine.Price * quantity;

            if (lineTotal > MaxSaleAmount ||
                totalAmount > MaxSaleAmount - lineTotal)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(
                    string.Empty,
                    "Итоговая сумма продажи превышает допустимое значение.");
                await LoadAvailableMedicinesAsync(model);
                return View(model);
            }

            totalAmount += lineTotal;
            saleItems.Add(new SaleItem
            {
                MedicineId = medicine.Id,
                Quantity = quantity,
                Price = medicine.Price,
                TotalPrice = lineTotal
            });
        }

        // Условное обновление не позволяет продать остаток, который уже
        // успела списать параллельная продажа.
        foreach (var item in saleItems)
        {
            var updatedRows = await _context.Medicines
                .Where(medicine =>
                    medicine.Id == item.MedicineId &&
                    medicine.IsActive &&
                    medicine.ExpirationDate >= today &&
                    medicine.Quantity >= item.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        medicine => medicine.Quantity,
                        medicine => medicine.Quantity - item.Quantity));

            if (updatedRows != 1)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(
                    string.Empty,
                    "Остаток одного из товаров изменился. Проверьте количество и оформите продажу ещё раз.");
                await LoadAvailableMedicinesAsync(model);
                return View(model);
            }
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Не удалось определить пользователя.");

        var sale = new Sale
        {
            UserId = userId,
            TotalAmount = totalAmount,
            Items = saleItems
        };

        _context.Sales.Add(sale);

        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(
                string.Empty,
                "Не удалось сохранить продажу. Проверьте данные и попробуйте ещё раз.");
            await LoadAvailableMedicinesAsync(model);
            return View(model);
        }

        TempData["Success"] = $"Продажа оформлена. Итого: {totalAmount:N2} руб.";
        return RedirectToAction(nameof(Create));
    }
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var query = _context.Sales
            .AsNoTracking()
            .AsQueryable();

        // Администратор видит все продажи, фармацевт — только свои.
        if (!User.IsInRole(RoleNames.Administrator))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            query = query.Where(sale => sale.UserId == userId);
        }

        var sales = await query
            .OrderByDescending(sale => sale.SaleDate)
            .Select(sale => new SalesListItemViewModel
            {
                Id = sale.Id,
                SaleDate = sale.SaleDate,
                UserFullName = sale.User!.FullName,
                UserName = sale.User.UserName,
                TotalAmount = sale.TotalAmount,
                Status = sale.Status
            })
            .ToListAsync();

        return View(sales);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var query = _context.Sales
            .AsNoTracking()
            .Where(sale => sale.Id == id.Value);

        // Проверка выполняется на сервере, поэтому фармацевт не сможет
        // открыть чужую продажу, просто изменив номер в адресной строке.
        if (!User.IsInRole(RoleNames.Administrator))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            query = query.Where(sale => sale.UserId == userId);
        }

        var sale = await query
            .Select(s => new SaleDetailsViewModel
            {
                Id = s.Id,
                SaleDate = s.SaleDate,
                UserFullName = s.User!.FullName,
                UserName = s.User.UserName,
                TotalAmount = s.TotalAmount,
                Status = s.Status,
                Items = s.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new SaleDetailsItemViewModel
                    {
                        MedicineName = item.Medicine!.Name,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        TotalPrice = item.TotalPrice
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (sale == null)
            return NotFound();

        return View(sale);
    }

    private async Task LoadAvailableMedicinesAsync(CreateSaleViewModel model)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        model.AvailableMedicines = await _context.Medicines
            .AsNoTracking()
            .Where(medicine =>
                medicine.IsActive &&
                medicine.Quantity > 0 &&
                medicine.ExpirationDate >= today)
            .OrderBy(medicine => medicine.Name)
            .Select(medicine => new SaleMedicineOptionViewModel
            {
                Id = medicine.Id,
                Name = medicine.Name,
                Price = medicine.Price,
                Stock = medicine.Quantity
            })
            .ToListAsync();

        if (model.Items.Count == 0)
            model.Items.Add(new SaleLineInputViewModel());
    }
}