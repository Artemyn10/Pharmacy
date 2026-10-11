using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Pharmacy.Models;

// Наш пользователь. Наследуется от IdentityUser, поэтому уже содержит:
// Id, UserName (логин), Email, PasswordHash и т.д.
public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "Укажите ФИО.")]
    [StringLength(150, ErrorMessage = "ФИО не должно быть длиннее 150 символов.")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    // Для блокировки пользователя администратором
    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;

    // Продажи, оформленные этим пользователем
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}