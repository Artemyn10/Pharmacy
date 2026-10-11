using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Введите текущий пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Текущий пароль")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите новый пароль.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать от 6 до 100 символов.")]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите новый пароль.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Новый пароль и подтверждение не совпадают.")]
    [Display(Name = "Подтверждение нового пароля")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}