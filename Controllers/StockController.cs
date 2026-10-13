using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;
using Pharmacy.Models.ViewModels;
using Pharmacy.Services;

namespace Pharmacy.Controllers;

// Остатки видят администратор и фармацевт
[Authorize(Roles = RoleNames.AdminOrPharmacist)]
public class StockController : Controller
{
    private readonly MedicineListService _medicineList;

    public StockController(MedicineListService medicineList)
    {
        _medicineList = medicineList;
    }

    // ---------- Остатки: /Stock ----------
    public async Task<IActionResult> Index([FromQuery] MedicineFilter filter)
    {
        var model = await _medicineList.GetListAsync(filter, onlyActive: true);
        return View(model);
    }
}