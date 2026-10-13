using Microsoft.AspNetCore.Mvc.Rendering;

namespace Pharmacy.Models.ViewModels;

// Параметры поиска и фильтров. Заполняются из адреса страницы (?Search=...&CategoryId=...)
public class MedicineFilter
{
    // Текст поиска: название или штрихкод
    public string? Search { get; set; }

    // null = «все категории» / «все производители»
    public int? CategoryId { get; set; }
    public int? ManufacturerId { get; set; }

    // all - все, in - в наличии, low - заканчивается, out - нет в наличии
    public string Availability { get; set; } = "all";

    // all - все, without - без рецепта, with - по рецепту
    public string Prescription { get; set; } = "all";

    // all - любой, expired - истёк, soon - скоро истекает, valid - в порядке
    public string Expiration { get; set; } = "all";
}

// Всё, что нужно странице: найденные товары, текущий фильтр и списки для выпадающих полей
public class MedicineListViewModel
{
    public MedicineFilter Filter { get; set; } = new();

    public List<Medicine> Medicines { get; set; } = new();

    public SelectList Categories { get; set; } = new SelectList(Array.Empty<Category>());

    public SelectList Manufacturers { get; set; } = new SelectList(Array.Empty<Manufacturer>());
}