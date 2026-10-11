using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название категории.")]
    [StringLength(100, ErrorMessage = "Название не должно быть длиннее 100 символов.")]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Описание не должно быть длиннее 500 символов.")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    // Одна категория -> много лекарств
    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}