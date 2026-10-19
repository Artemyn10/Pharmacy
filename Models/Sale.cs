using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Pharmacy.Models;
public static class SaleStatuses
{
    public const string Completed = "Завершена";
    public const string Cancelled = "Отменена";
}
public class Sale
{
    public int Id { get; set; }

    // Кто оформил продажу. У Identity ключ пользователя - строка (string)
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    // Храним в UTC, на странице будем показывать в местном времени
    [Display(Name = "Дата")]
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    [Precision(12, 2)]
    [Display(Name = "Итого, руб.")]
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Статус")]
    public string Status { get; set; } = SaleStatuses.Completed;

    // Состав продажи: одна продажа -> много позиций
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}