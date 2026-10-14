using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Pharmacy.Models;

public static class CustomerOrderStatuses
{
    public const string New = "Новый";
    public const string Processing = "В обработке";
    public const string Completed = "Завершён";
    public const string Cancelled = "Отменён";
}

public class CustomerOrder
{
    public int Id { get; set; }

    // Пользователь-клиент, создавший заказ.
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Display(Name = "Дата")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Precision(12, 2)]
    [Display(Name = "Итого, руб.")]
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Статус")]
    public string Status { get; set; } = CustomerOrderStatuses.New;

    public ICollection<CustomerOrderItem> Items { get; set; } = new List<CustomerOrderItem>();
}