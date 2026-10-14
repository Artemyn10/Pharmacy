using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models.ViewModels;

public class CreateSaleViewModel
{
    [MinLength(1, ErrorMessage = "Добавьте хотя бы один товар.")]
    public List<SaleLineInputViewModel> Items { get; set; } =
        new() { new SaleLineInputViewModel() };

    // Список для выбора. При отправке формы сервер загружает его заново.
    public List<SaleMedicineOptionViewModel> AvailableMedicines { get; set; } = new();
}

public class SaleLineInputViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Выберите товар.")]
    public int MedicineId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не меньше 1.")]
    [Display(Name = "Количество")]
    public int Quantity { get; set; } = 1;
}

public class SaleMedicineOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}