using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models.ViewModels;

public class CreateCustomerOrderViewModel
{
    [MinLength(1, ErrorMessage = "Добавьте хотя бы один товар.")]
    public List<CustomerOrderLineInputViewModel> Items { get; set; } =
        new() { new CustomerOrderLineInputViewModel() };

    public List<CustomerOrderMedicineOptionViewModel> AvailableMedicines { get; set; } = new();
}

public class CustomerOrderLineInputViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Выберите товар.")]
    public int MedicineId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не меньше 1.")]
    [Display(Name = "Количество")]
    public int Quantity { get; set; } = 1;
}

public class CustomerOrderMedicineOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class CustomerOrderListItemViewModel
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class CustomerOrderDetailsViewModel
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsStaff { get; set; }
    public bool CanCancel { get; set; }
    public List<CustomerOrderDetailsItemViewModel> Items { get; set; } = new();
}

public class CustomerOrderDetailsItemViewModel
{
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}