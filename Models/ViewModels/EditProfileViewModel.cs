using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models.ViewModels;

public class EditProfileViewModel
{
    [Required(ErrorMessage = "Укажите ФИО.")]
    [StringLength(150, ErrorMessage = "ФИО не должно быть длиннее 150 символов.")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите адрес электронной почты.")]
    [EmailAddress(ErrorMessage = "Введите корректный адрес электронной почты.")]
    [Display(Name = "Электронная почта")]
    public string Email { get; set; } = string.Empty;
}