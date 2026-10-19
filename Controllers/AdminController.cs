using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;
using Pharmacy.Models.ViewModels;

namespace Pharmacy.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var todayUtc = today.ToUniversalTime();
        var tomorrowUtc = tomorrow.ToUniversalTime();
        var todayDate = DateOnly.FromDateTime(DateTime.Today);

        var model = new AdminDashboardViewModel
        {
            MedicineCount = await _context.Medicines
                .CountAsync(medicine => medicine.IsActive),

            CategoryCount = await _context.Categories.CountAsync(),

            UserCount = await _userManager.Users.CountAsync(),

            SaleCount = await _context.Sales
            .CountAsync(sale => sale.Status == SaleStatuses.Completed),

            SalesTodayCount = await _context.Sales.CountAsync(sale =>
                sale.Status == SaleStatuses.Completed &&
                sale.SaleDate >= todayUtc &&
                sale.SaleDate < tomorrowUtc),

            TotalSalesAmount = await _context.Sales
            .Where(sale => sale.Status == SaleStatuses.Completed)
            .Select(sale => sale.TotalAmount)
            .SumAsync(),

            LowStockCount = await _context.Medicines.CountAsync(medicine =>
                medicine.IsActive &&
                medicine.ExpirationDate >= todayDate &&
                medicine.Quantity > 0 &&
                medicine.Quantity < 10)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .ToListAsync();

        var model = new List<AdminUserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            model.Add(new AdminUserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = RoleNames.GetDisplayName(roles.FirstOrDefault()),
                IsActive = user.IsActive
            });
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> UserDetails(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return View(new AdminUserListItemViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = RoleNames.GetDisplayName(roles.FirstOrDefault()),
            IsActive = user.IsActive
        });
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return View(new AdminUserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? RoleNames.Client,
            IsActive = user.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(AdminUserEditViewModel model)
    {
        var allowedRoles = new[]
        {
            RoleNames.Administrator,
            RoleNames.Pharmacist,
            RoleNames.Client
        };

        if (!allowedRoles.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Выбрана неизвестная роль.");

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
            return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault();
        var roleChanged = currentRoles.Count != 1 || currentRole != model.Role;
        var activeChanged = user.IsActive != model.IsActive;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Id == currentUserId && (roleChanged || !model.IsActive))
        {
            ModelState.AddModelError(
                string.Empty,
                "Нельзя изменить свою роль или деактивировать собственную учётную запись.");
            return View(model);
        }

        var roleExists = await _context.Roles
            .AnyAsync(role => role.Name == model.Role);
        if (!roleExists)
        {
            ModelState.AddModelError(nameof(model.Role), "Эта роль не зарегистрирована в системе.");
            return View(model);
        }

        var anotherUserWithEmail = await _userManager.FindByEmailAsync(model.Email);
        if (anotherUserWithEmail != null && anotherUserWithEmail.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.Email), "Этот email уже используется.");
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await transaction.RollbackAsync();
            AddIdentityErrors(updateResult);
            return View(model);
        }

        if (roleChanged)
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    AddIdentityErrors(removeResult);
                    return View(model);
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!addResult.Succeeded)
            {
                await transaction.RollbackAsync();
                AddIdentityErrors(addResult);
                return View(model);
            }
        }

        if (roleChanged || activeChanged)
        {
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                await transaction.RollbackAsync();
                AddIdentityErrors(stampResult);
                return View(model);
            }
        }

        await transaction.CommitAsync();

        TempData["Success"] = "Данные пользователя сохранены.";
        return RedirectToAction(nameof(UserDetails), new { id = user.Id });
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }
}