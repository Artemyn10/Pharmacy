using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;
using Pharmacy.Models.ViewModels;

namespace Pharmacy.Controllers;

[Authorize(Roles = RoleNames.Client + "," + RoleNames.Administrator + "," + RoleNames.Pharmacist)]
public class OrdersController : Controller
{
    private const decimal MaxOrderAmount = 9_999_999_999.99m;
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateCustomerOrderViewModel();
        await LoadAvailableMedicinesAsync(model);
        return View(model);
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCustomerOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadAvailableMedicinesAsync(model);
            return View(model);
        }

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
                "Один из товаров недоступен для заказа или просрочен.");
            await LoadAvailableMedicinesAsync(model);
            return View(model);
        }

        var orderItems = new List<CustomerOrderItem>();
        decimal totalAmount = 0;

        foreach (var requested in requestedItems)
        {
            var medicine = medicines[requested.MedicineId];
            var quantity = (int)requested.Quantity;
            var lineTotal = medicine.Price * quantity;

            if (lineTotal > MaxOrderAmount ||
                totalAmount > MaxOrderAmount - lineTotal)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(
                    string.Empty,
                    "Итоговая сумма заказа превышает допустимое значение.");
                await LoadAvailableMedicinesAsync(model);
                return View(model);
            }

            totalAmount += lineTotal;
            orderItems.Add(new CustomerOrderItem
            {
                MedicineId = medicine.Id,
                MedicineName = medicine.Name,
                Quantity = quantity,
                UnitPrice = medicine.Price,
                TotalPrice = lineTotal
            });
        }

        // Количество списывается сразу и тем самым резервируется.
        // Условное обновление защищает остаток от параллельных заказов.
        foreach (var item in orderItems)
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
                    "Остаток одного из товаров изменился. Проверьте количество и отправьте заказ повторно.");
                await LoadAvailableMedicinesAsync(model);
                return View(model);
            }
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            throw new InvalidOperationException("Не удалось определить пользователя.");

        var order = new CustomerOrder
        {
            UserId = userId,
            TotalAmount = totalAmount,
            Status = CustomerOrderStatuses.New,
            Items = orderItems
        };

        _context.CustomerOrders.Add(order);

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
                "Не удалось сохранить заказ. Проверьте данные и попробуйте ещё раз.");
            await LoadAvailableMedicinesAsync(model);
            return View(model);
        }

        TempData["Success"] = $"Заказ #{order.Id} создан. Итого: {totalAmount:N2} руб.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var query = _context.CustomerOrders
            .AsNoTracking()
            .AsQueryable();

        // Клиент получает только свои заказы. Сотрудники видят очередь заказов.
        if (User.IsInRole(RoleNames.Client))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Forbid();

            query = query.Where(order => order.UserId == userId);
        }

        var orders = await query
            .OrderByDescending(order => order.OrderDate)
            .Select(order => new CustomerOrderListItemViewModel
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                CustomerName = order.User!.FullName,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                ItemCount = order.Items.Count
            })
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var query = _context.CustomerOrders
            .AsNoTracking()
            .Where(order => order.Id == id.Value);

        if (User.IsInRole(RoleNames.Client))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Forbid();

            query = query.Where(order => order.UserId == userId);
        }

        var isStaff =
            User.IsInRole(RoleNames.Administrator) ||
            User.IsInRole(RoleNames.Pharmacist);

        var order = await query
            .Select(o => new CustomerOrderDetailsViewModel
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                CustomerName = o.User!.FullName,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                IsStaff = isStaff,
                CanCancel = o.Status == CustomerOrderStatuses.New ||
                    (isStaff && o.Status == CustomerOrderStatuses.Processing),
                Items = o.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new CustomerOrderDetailsItemViewModel
                    {
                        MedicineName = item.MedicineName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound();

        return View(order);
    }

    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Pharmacist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, string newStatus)
    {
        var order = await _context.CustomerOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (order == null)
            return NotFound();

        string? allowedNextStatus = order.Status switch
        {
            CustomerOrderStatuses.New => CustomerOrderStatuses.Processing,
            CustomerOrderStatuses.Processing => CustomerOrderStatuses.Completed,
            _ => null
        };

        if (newStatus != allowedNextStatus)
        {
            TempData["Error"] = "Недопустимый переход статуса заказа.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var updatedRows = await _context.CustomerOrders
            .Where(item => item.Id == id && item.Status == order.Status)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, newStatus));

        TempData[updatedRows == 1 ? "Success" : "Error"] = updatedRows == 1
            ? "Статус заказа обновлён."
            : "Заказ уже изменён. Обновите страницу.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var query = _context.CustomerOrders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.Id == id);

        var isClient = User.IsInRole(RoleNames.Client);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (isClient)
        {
            if (userId == null)
                return Forbid();

            query = query.Where(order => order.UserId == userId);
        }

        var order = await query.FirstOrDefaultAsync();
        if (order == null)
            return NotFound();

        var canCancel = order.Status == CustomerOrderStatuses.New ||
            (!isClient && order.Status == CustomerOrderStatuses.Processing);

        if (!canCancel)
        {
            TempData["Error"] = "Этот заказ уже нельзя отменить.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Только один запрос может отменить заказ и вернуть остатки.
        var updatedRows = await _context.CustomerOrders
            .Where(item => item.Id == id && item.Status == order.Status)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, CustomerOrderStatuses.Cancelled));

        if (updatedRows != 1)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Заказ уже изменён. Обновите страницу.";
            return RedirectToAction(nameof(Details), new { id });
        }

        foreach (var item in order.Items)
        {
            await _context.Medicines
                .Where(medicine => medicine.Id == item.MedicineId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        medicine => medicine.Quantity,
                        medicine => medicine.Quantity + item.Quantity));
        }

        await transaction.CommitAsync();

        TempData["Success"] = "Заказ отменён, остаток товара возвращён на склад.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadAvailableMedicinesAsync(CreateCustomerOrderViewModel model)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        model.AvailableMedicines = await _context.Medicines
            .AsNoTracking()
            .Where(medicine =>
                medicine.IsActive &&
                medicine.Quantity > 0 &&
                medicine.ExpirationDate >= today)
            .OrderBy(medicine => medicine.Name)
            .Select(medicine => new CustomerOrderMedicineOptionViewModel
            {
                Id = medicine.Id,
                Name = medicine.Name,
                Price = medicine.Price,
                Stock = medicine.Quantity
            })
            .ToListAsync();

        if (model.Items.Count == 0)
            model.Items.Add(new CustomerOrderLineInputViewModel());
    }
}