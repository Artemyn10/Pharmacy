using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models;

public class Manufacturer
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название производителя.")]
    [StringLength(150, ErrorMessage = "Название не должно быть длиннее 150 символов.")]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите страну.")]
    [StringLength(100, ErrorMessage = "Название страны не должно быть длиннее 100 символов.")]
    [Display(Name = "Страна")]
    public string Country { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Описание не должно быть длиннее 500 символов.")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}