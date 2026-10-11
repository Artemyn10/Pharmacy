using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;
using Pharmacy.Models.ViewModels;

namespace Pharmacy.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ---------- Регистрация ----------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Проверка уникальности логина
        if (await _userManager.FindByNameAsync(model.UserName) != null)
        {
            ModelState.AddModelError(nameof(model.UserName), "Пользователь с таким логином уже существует.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FullName = model.FullName,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            // Ошибки Identity (пароль, дубликат email и т.д.) показываем в форме
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // Роль при самостоятельной регистрации всегда "Клиент"
        await _userManager.AddToRoleAsync(user, RoleNames.Client);

        // Сразу входим в систему
        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["Success"] = "Регистрация прошла успешно. Добро пожаловать!";
        return RedirectToAction("Index", "Home");
    }

    // ---------- Вход ----------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByNameAsync(model.UserName);

        // Проверяем логин и пароль
        if (user == null ||
            !(await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false)).Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            return View(model);
        }

        // Заблокированных пользователей не пускаем
        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Ваша учётная запись заблокирована. Обратитесь к администратору.");
            return View(model);
        }

        await _signInManager.SignInAsync(user, model.RememberMe);

        // Возвращаем на страницу, куда человек шёл (только внутри нашего сайта)
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ---------- Выход ----------

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        TempData["Success"] = "Вы вышли из системы.";
        return RedirectToAction("Index", "Home");
    }

    // ---------- Доступ запрещён ----------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}