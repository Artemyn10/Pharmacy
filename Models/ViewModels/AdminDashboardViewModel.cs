namespace Pharmacy.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int MedicineCount { get; set; }
    public int CategoryCount { get; set; }
    public int UserCount { get; set; }
    public int SaleCount { get; set; }
    public int SalesTodayCount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public int LowStockCount { get; set; }
}