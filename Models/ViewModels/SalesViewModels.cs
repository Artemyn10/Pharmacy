namespace Pharmacy.Models.ViewModels;

public class SalesListItemViewModel
{
    public int Id { get; set; }
    public DateTime SaleDate { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SaleDetailsViewModel
{
    public int Id { get; set; }
    public DateTime SaleDate { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<SaleDetailsItemViewModel> Items { get; set; } = new();
}

public class SaleDetailsItemViewModel
{
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice { get; set; }
}