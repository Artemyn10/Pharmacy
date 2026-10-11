using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Pharmacy.Models;

public class Medicine
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название товара.")]
    [StringLength(200, ErrorMessage = "Название не должно быть длиннее 200 символов.")]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Описание не должно быть длиннее 2000 символов.")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    // ---- Внешние ключи и связи ----

    [Range(1, int.MaxValue, ErrorMessage = "Выберите категорию.")]
    [Display(Name = "Категория")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите производителя.")]
    [Display(Name = "Производитель")]
    public int ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите поставщика.")]
    [Display(Name = "Поставщик")]
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    // ---- Основные данные ----

    [Precision(10, 2)] // до 99 999 999,99 с двумя знаками после запятой
    [Range(0.01, 1000000, ErrorMessage = "Цена должна быть больше 0.")]
    [Display(Name = "Цена, руб.")]
    public decimal Price { get; set; }

    // Текущий остаток на складе
    [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным.")]
    [Display(Name = "Количество")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Укажите срок годности.")]
    [DataType(DataType.Date)]
    [Display(Name = "Срок годности")]
    public DateOnly ExpirationDate { get; set; }

    [Required(ErrorMessage = "Укажите штрихкод.")]
    [StringLength(50, ErrorMessage = "Штрихкод не должен быть длиннее 50 символов.")]
    [Display(Name = "Штрихкод")]
    public string Barcode { get; set; } = string.Empty;

    [Display(Name = "Требуется рецепт")]
    public bool PrescriptionRequired { get; set; }

    // false = товар деактивирован (не удаляем физически, чтобы не потерять историю продаж)
    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}