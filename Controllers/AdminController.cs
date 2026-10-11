using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;

namespace Pharmacy.Controllers;

// Атрибут на классе защищает ВСЕ действия контроллера сразу.
// Войти сюда может только пользователь с ролью Administrator.
[Authorize(Roles = RoleNames.Administrator)]
public class AdminController : Controller
{
    // /Admin
    public IActionResult Index()
    {
        return View();
    }

    // /Admin/Users
    public IActionResult Users()
    {
        return View();
    }
}