using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models.ViewModels;

public class AdminUserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AdminUserEditViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите ФИО.")]
    [StringLength(150, ErrorMessage = "ФИО не должно быть длиннее 150 символов.")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите email.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите роль.")]
    [Display(Name = "Роль")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Активен")]
    public bool IsActive { get; set; }
}