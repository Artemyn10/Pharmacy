using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;

namespace Pharmacy.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class ManufacturersController : Controller
{
    private readonly ApplicationDbContext _context;

    public ManufacturersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ---------- Список: /Manufacturers ----------
    public async Task<IActionResult> Index()
    {
        var manufacturers = await _context.Manufacturers
            .AsNoTracking()
            .Include(m => m.Medicines)
            .OrderBy(m => m.Name)
            .ToListAsync();

        return View(manufacturers);
    }

    // ---------- Добавление ----------

    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new Manufacturer());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Country,Description")] Manufacturer manufacturer)
    {
        if (!ModelState.IsValid)
            return View("Form", manufacturer);

        try
        {
            _context.Manufacturers.Add(manufacturer);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Не удалось сохранить данные.");
            return View("Form", manufacturer);
        }

        TempData["Success"] = "Производитель успешно добавлен.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Изменение ----------

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var manufacturer = await _context.Manufacturers.FindAsync(id);
        if (manufacturer == null)
            return NotFound();

        return View("Form", manufacturer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Country,Description")] Manufacturer manufacturer)
    {
        if (id != manufacturer.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View("Form", manufacturer);

        try
        {
            _context.Manufacturers.Update(manufacturer);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Не удалось сохранить данные. Возможно, производитель был удалён.");
            return View("Form", manufacturer);
        }

        TempData["Success"] = "Изменения успешно сохранены.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Удаление ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var manufacturer = await _context.Manufacturers.FindAsync(id);
        if (manufacturer == null)
            return NotFound();

        var medicinesCount = await _context.Medicines.CountAsync(m => m.ManufacturerId == id);
        if (medicinesCount > 0)
        {
            TempData["Error"] = $"Нельзя удалить производителя «{manufacturer.Name}»: к нему привязано товаров: {medicinesCount}.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.Manufacturers.Remove(manufacturer);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Не удалось удалить производителя.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Производитель успешно удалён.";
        return RedirectToAction(nameof(Index));
    }
}