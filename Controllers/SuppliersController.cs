using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;

namespace Pharmacy.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class SuppliersController : Controller
{
    private readonly ApplicationDbContext _context;

    public SuppliersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ---------- Список: /Suppliers ----------
    public async Task<IActionResult> Index()
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.Medicines)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return View(suppliers);
    }

    // ---------- Добавление ----------

    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new Supplier());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Phone,Email,Address")] Supplier supplier)
    {
        if (!ModelState.IsValid)
            return View("Form", supplier);

        try
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Не удалось сохранить данные.");
            return View("Form", supplier);
        }

        TempData["Success"] = "Поставщик успешно добавлен.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Изменение ----------

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound();

        return View("Form", supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Phone,Email,Address")] Supplier supplier)
    {
        if (id != supplier.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View("Form", supplier);

        try
        {
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Не удалось сохранить данные. Возможно, поставщик был удалён.");
            return View("Form", supplier);
        }

        TempData["Success"] = "Изменения успешно сохранены.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Удаление ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound();

        var medicinesCount = await _context.Medicines.CountAsync(m => m.SupplierId == id);
        if (medicinesCount > 0)
        {
            TempData["Error"] = $"Нельзя удалить поставщика «{supplier.Name}»: к нему привязано товаров: {medicinesCount}.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Не удалось удалить поставщика.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Поставщик успешно удалён.";
        return RedirectToAction(nameof(Index));
    }
}