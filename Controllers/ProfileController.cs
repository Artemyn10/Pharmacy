using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;
using Pharmacy.Models.ViewModels;

namespace Pharmacy.Controllers;

// Весь контроллер доступен только авторизованным пользователям (любая роль)
[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ---------- Просмотр: /Profile ----------

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return await SignOutAndRedirectAsync();

        var roles = await _userManager.GetRolesAsync(user);

        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = RoleNames.GetDisplayName(roles.FirstOrDefault())
        };

        return View(model);
    }

    // ---------- Изменение данных: /Profile/Edit ----------

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return await SignOutAndRedirectAsync();

        var model = new EditProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return await SignOutAndRedirectAsync();

        // Проверка: этот email не должен принадлежать другому пользователю
        var userWithSameEmail = await _userManager.FindByEmailAsync(model.Email);
        if (userWithSameEmail != null && userWithSameEmail.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.Email), "Этот адрес электронной почты уже используется.");
            return View(model);
        }

        user.FullName = model.FullName;
        user.Email = model.Email;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        TempData["Success"] = "Данные успешно сохранены.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Смена пароля: /Profile/ChangePassword ----------

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return await SignOutAndRedirectAsync();

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // После смены пароля обновляем cookie, чтобы пользователя не «выкинуло» из системы
        await _signInManager.RefreshSignInAsync(user);

        TempData["Success"] = "Пароль успешно изменён.";
        return RedirectToAction(nameof(Index));
    }

    // Если пользователя в базе уже нет (например, удалён), завершаем сеанс
    private async Task<IActionResult> SignOutAndRedirectAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }
}