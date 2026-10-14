using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Pharmacy.Models;

public class CustomerOrderItem
{
    public int Id { get; set; }

    public int CustomerOrderId { get; set; }
    public CustomerOrder? CustomerOrder { get; set; }

    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    // Сохраняем название и цену на момент заказа.
    [Required]
    [StringLength(200)]
    public string MedicineName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Precision(10, 2)]
    public decimal UnitPrice { get; set; }

    [Precision(12, 2)]
    public decimal TotalPrice { get; set; }
}