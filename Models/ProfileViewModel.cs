using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models.ViewModels;

public class ProfileViewModel
{
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Логин")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Электронная почта")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Роль")]
    public string Role { get; set; } = string.Empty;
}