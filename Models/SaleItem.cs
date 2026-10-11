using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Pharmacy.Models;

// Одна строка в продаже: «Парацетамол - 2 шт. - 50 руб.»
public class SaleItem
{
    public int Id { get; set; }

    public int SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не меньше 1.")]
    [Display(Name = "Количество")]
    public int Quantity { get; set; }

    // Цена на момент продажи. Копируем из Medicine, чтобы при будущем
    // изменении цены старые продажи не пересчитывались.
    [Precision(10, 2)]
    [Display(Name = "Цена, руб.")]
    public decimal Price { get; set; }

    // Quantity * Price
    [Precision(12, 2)]
    [Display(Name = "Сумма, руб.")]
    public decimal TotalPrice { get; set; }
}