using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Укажите логин.")]
    [Display(Name = "Логин")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }
}