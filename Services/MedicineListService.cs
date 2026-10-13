using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;
using Pharmacy.Models.ViewModels;

namespace Pharmacy.Services;

// Находит лекарства по поиску и фильтрам.
// Используется и в каталоге, и на странице «Остатки».
public class MedicineListService
{
    private readonly ApplicationDbContext _context;

    public MedicineListService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MedicineListViewModel> GetListAsync(MedicineFilter filter, bool onlyActive)
    {
        IQueryable<Medicine> query = _context.Medicines
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Manufacturer);

        if (onlyActive)
            query = query.Where(m => m.IsActive);

        query = ApplyFilter(query, filter);

        var medicines = await query.OrderBy(m => m.Name).ToListAsync();

        // Списки для выпадающих полей фильтра
        var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var manufacturers = await _context.Manufacturers.AsNoTracking().OrderBy(m => m.Name).ToListAsync();

        return new MedicineListViewModel
        {
            Filter = filter,
            Medicines = medicines,
            Categories = new SelectList(categories, "Id", "Name", filter.CategoryId),
            Manufacturers = new SelectList(manufacturers, "Id", "Name", filter.ManufacturerId)
        };
    }

    // Добавляет к запросу условия из фильтра. Каждое условие необязательное:
    // если поле фильтра не заполнено, оно просто пропускается.
    private static IQueryable<Medicine> ApplyFilter(IQueryable<Medicine> query, MedicineFilter filter)
    {
        // ----- Поиск по названию или штрихкоду -----
        var term = filter.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{EscapeLike(term)}%";

            // ILike в PostgreSQL ищет по части строки без учёта регистра
            query = query.Where(m =>
                EF.Functions.ILike(m.Name, pattern) ||
                EF.Functions.ILike(m.Barcode, pattern));
        }

        // ----- Категория и производитель -----
        if (filter.CategoryId.HasValue)
            query = query.Where(m => m.CategoryId == filter.CategoryId.Value);

        if (filter.ManufacturerId.HasValue)
            query = query.Where(m => m.ManufacturerId == filter.ManufacturerId.Value);

        // ----- Наличие -----
        query = filter.Availability switch
        {
            "in" => query.Where(m => m.Quantity > 0),
            "low" => query.Where(m => m.Quantity > 0 && m.Quantity < MedicineRules.LowStockThreshold),
            "out" => query.Where(m => m.Quantity <= 0),
            _ => query
        };

        // ----- Рецепт -----
        query = filter.Prescription switch
        {
            "with" => query.Where(m => m.PrescriptionRequired),
            "without" => query.Where(m => !m.PrescriptionRequired),
            _ => query
        };

        // ----- Срок годности -----
        var today = DateOnly.FromDateTime(DateTime.Today);
        var soonLimit = today.AddDays(MedicineRules.ExpiringSoonDays);

        query = filter.Expiration switch
        {
            "expired" => query.Where(m => m.ExpirationDate < today),
            "soon" => query.Where(m => m.ExpirationDate >= today && m.ExpirationDate <= soonLimit),
            "valid" => query.Where(m => m.ExpirationDate > soonLimit),
            _ => query
        };

        return query;
    }

    // Если пользователь введёт в поиск символы %, _ или \, считаем их обычными буквами
    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}