using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;

namespace Pharmacy.Controllers;

// Весь контроллер только для администратора
[Authorize(Roles = RoleNames.Administrator)]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ---------- Список: /Categories ----------
    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Medicines)   // чтобы показать количество товаров
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }

    // ---------- Добавление ----------

    [HttpGet]
    public IActionResult Create()
    {
        // Пустая категория, форма «Form» одна и для добавления, и для изменения
        return View("Form", new Category());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
    {
        await NormalizeAndCheckNameAsync(category);

        if (!ModelState.IsValid)
            return View("Form", category);

        try
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Не удалось сохранить данные.");
            return View("Form", category);
        }

        TempData["Success"] = "Категория успешно добавлена.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Изменение ----------

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();

        return View("Form", category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Category category)
    {
        if (id != category.Id)
            return NotFound();

        await NormalizeAndCheckNameAsync(category);

        if (!ModelState.IsValid)
            return View("Form", category);

        try
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Не удалось сохранить данные. Возможно, категория была удалена.");
            return View("Form", category);
        }

        TempData["Success"] = "Изменения успешно сохранены.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Удаление ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();

        // Проверяем, есть ли товары в этой категории
        var medicinesCount = await _context.Medicines.CountAsync(m => m.CategoryId == id);
        if (medicinesCount > 0)
        {
            TempData["Error"] = $"Нельзя удалить категорию «{category.Name}»: к ней привязано товаров: {medicinesCount}.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Не удалось удалить категорию.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Категория успешно удалена.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Вспомогательный метод ----------

    // Убирает лишние пробелы и проверяет, что такого названия ещё нет
    private async Task NormalizeAndCheckNameAsync(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
            return; // ошибку «Укажите название» покажет атрибут [Required]

        category.Name = category.Name.Trim();
        var name = category.Name.ToLower();

        var exists = await _context.Categories
            .AnyAsync(c => c.Id != category.Id && c.Name.ToLower() == name);

        if (exists)
            ModelState.AddModelError(nameof(category.Name), "Категория с таким названием уже существует.");
    }
}