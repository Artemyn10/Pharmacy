using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models;

public class Supplier
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название поставщика.")]
    [StringLength(150, ErrorMessage = "Название не должно быть длиннее 150 символов.")]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Введите корректный номер телефона.")]
    [StringLength(30, ErrorMessage = "Телефон не должен быть длиннее 30 символов.")]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Введите корректный адрес электронной почты.")]
    [StringLength(100, ErrorMessage = "Email не должен быть длиннее 100 символов.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(250, ErrorMessage = "Адрес не должен быть длиннее 250 символов.")]
    [Display(Name = "Адрес")]
    public string? Address { get; set; }

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}