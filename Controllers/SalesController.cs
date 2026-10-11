using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;

namespace Pharmacy.Controllers;

// Доступ для администратора ИЛИ фармацевта
[Authorize(Roles = RoleNames.AdminOrPharmacist)]
public class SalesController : Controller
{
    // /Sales/Create (заглушка, настоящую страницу сделаем на Этапе 15)
    public IActionResult Create()
    {
        return View();
    }
}