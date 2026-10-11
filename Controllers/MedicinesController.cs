using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;

namespace Pharmacy.Controllers;

// ВСЕ действия по умолчанию доступны только администратору.
// Публичные страницы каталога помечены [AllowAnonymous].
[Authorize(Roles = RoleNames.Administrator)]
public class MedicinesController : Controller
{
    private readonly ApplicationDbContext _context;

    public MedicinesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =====================================================
    //  ПУБЛИЧНАЯ ЧАСТЬ: каталог (доступен всем)
    // =====================================================

    // ---------- Каталог: /Medicines ----------
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var medicines = await _context.Medicines
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Manufacturer)
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync();

        return View(medicines);
    }

    // ---------- Подробнее: /Medicines/Details/5 ----------
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var medicine = await _context.Medicines
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Manufacturer)
            .FirstOrDefaultAsync(m => m.Id == id);

        // Товара нет, либо он деактивирован (деактивированные видит только администратор)
        if (medicine == null ||
            (!medicine.IsActive && !User.IsInRole(RoleNames.Administrator)))
        {
            return NotFound();
        }

        return View(medicine);
    }

    // =====================================================
    //  ЧАСТЬ АДМИНИСТРАТОРА
    // =====================================================

    // ---------- Список всех товаров: /Medicines/Manage ----------
    public async Task<IActionResult> Manage()
    {
        var medicines = await _context.Medicines
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Manufacturer)
            .OrderBy(m => m.Name)
            .ToListAsync();

        return View(medicines);
    }

    // ---------- Добавление: /Medicines/Create ----------

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();

        // Значения по умолчанию для новой формы
        var medicine = new Medicine
        {
            ExpirationDate = DateOnly.FromDateTime(DateTime.Today).AddYears(1),
            IsActive = true
        };

        return View(medicine);
    }

    // [Bind(...)] перечисляет поля, которые разрешено принять из формы.
    // Остальное из запроса игнорируется (защита от подмены данных).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,CategoryId,ManufacturerId,SupplierId,Price,Quantity,ExpirationDate,Barcode,PrescriptionRequired,IsActive")]
        Medicine medicine)
    {
        // Штрихкод должен быть уникальным
        if (!string.IsNullOrWhiteSpace(medicine.Barcode) &&
            await _context.Medicines.AnyAsync(m => m.Barcode == medicine.Barcode))
        {
            ModelState.AddModelError(nameof(medicine.Barcode), "Товар с таким штрихкодом уже существует.");
        }

        // Нельзя добавить уже просроченный товар
        if (medicine.ExpirationDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(medicine.ExpirationDate), "Срок годности не может быть в прошлом.");
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(medicine);
            return View(medicine);
        }

        try
        {
            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Не удалось сохранить данные. Проверьте введённые значения и попробуйте ещё раз.");
            await LoadListsAsync(medicine);
            return View(medicine);
        }

        TempData["Success"] = "Товар успешно добавлен.";
        return RedirectToAction(nameof(Manage));
    }

    // ---------- Изменение: /Medicines/Edit/5 ----------

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var medicine = await _context.Medicines.FindAsync(id);
        if (medicine == null)
            return NotFound();

        await LoadListsAsync(medicine);
        return View(medicine);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Name,Description,CategoryId,ManufacturerId,SupplierId,Price,Quantity,ExpirationDate,Barcode,PrescriptionRequired,IsActive")]
        Medicine medicine)
    {
        if (id != medicine.Id)
            return NotFound();

        // Штрихкод уникален среди ДРУГИХ товаров
        if (!string.IsNullOrWhiteSpace(medicine.Barcode) &&
            await _context.Medicines.AnyAsync(m => m.Barcode == medicine.Barcode && m.Id != medicine.Id))
        {
            ModelState.AddModelError(nameof(medicine.Barcode), "Товар с таким штрихкодом уже существует.");
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(medicine);
            return View(medicine);
        }

        try
        {
            _context.Medicines.Update(medicine);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Сюда попадём, например, если товар успели удалить
            ModelState.AddModelError(string.Empty, "Не удалось сохранить данные. Возможно, товар был удалён или изменён.");
            await LoadListsAsync(medicine);
            return View(medicine);
        }

        TempData["Success"] = "Изменения успешно сохранены.";
        return RedirectToAction(nameof(Manage));
    }

    // ---------- Деактивация / активация ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var medicine = await _context.Medicines.FindAsync(id);
        if (medicine == null)
            return NotFound();

        medicine.IsActive = !medicine.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Не удалось сохранить данные.";
            return RedirectToAction(nameof(Manage));
        }

        TempData["Success"] = medicine.IsActive
            ? "Товар активирован и снова виден в каталоге."
            : "Товар деактивирован и скрыт из каталога.";

        return RedirectToAction(nameof(Manage));
    }

    // ---------- Удаление: /Medicines/Delete/5 ----------

    // Страница подтверждения
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var medicine = await _context.Medicines
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Manufacturer)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (medicine == null)
            return NotFound();

        // Если товар уже продавался, удалять его нельзя
        ViewBag.HasSales = await _context.SaleItems.AnyAsync(i => i.MedicineId == medicine.Id);

        return View(medicine);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var medicine = await _context.Medicines.FindAsync(id);
        if (medicine == null)
            return NotFound();

        // Нельзя удалить товар, который есть в истории продаж
        if (await _context.SaleItems.AnyAsync(i => i.MedicineId == id))
        {
            TempData["Error"] = "Нельзя удалить товар, который уже продавался. Деактивируйте его.";
            return RedirectToAction(nameof(Manage));
        }

        try
        {
            _context.Medicines.Remove(medicine);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Не удалось удалить товар.";
            return RedirectToAction(nameof(Manage));
        }

        TempData["Success"] = "Товар успешно удалён.";
        return RedirectToAction(nameof(Manage));
    }

    // ---------- Вспомогательный метод: списки для выпадающих полей ----------

    private async Task LoadListsAsync(Medicine? medicine = null)
    {
        var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var manufacturers = await _context.Manufacturers.AsNoTracking().OrderBy(m => m.Name).ToListAsync();
        var suppliers = await _context.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

        // SelectList(данные, поле-значение, поле-текст, выбранное значение)
        ViewBag.Categories = new SelectList(categories, "Id", "Name", medicine?.CategoryId);
        ViewBag.Manufacturers = new SelectList(manufacturers, "Id", "Name", medicine?.ManufacturerId);
        ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name", medicine?.SupplierId);
    }
}