using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Models;

namespace Pharmacy.Data;

// Заполняет базу начальными данными. Безопасно вызывать при каждом запуске:
// то, что уже есть, повторно не создаётся.
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);
        await SeedCatalogAsync(context);
    }

    // ---------- Роли ----------
    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { RoleNames.Administrator, RoleNames.Pharmacist, RoleNames.Client };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // ---------- Пользователи ----------
    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        await CreateUserAsync(userManager, "admin", "admin@pharmacy.local",
            "Администратор системы", "Admin123!", RoleNames.Administrator);

        await CreateUserAsync(userManager, "pharmacist", "pharmacist@pharmacy.local",
            "Петрова Анна Сергеевна", "Pharmacist123!", RoleNames.Pharmacist);

        await CreateUserAsync(userManager, "client1", "client1@pharmacy.local",
            "Смирнов Алексей Игоревич", "Client123!", RoleNames.Client);

        await CreateUserAsync(userManager, "client2", "client2@pharmacy.local",
            "Козлова Ольга Дмитриевна", "Client123!", RoleNames.Client);

        await CreateUserAsync(userManager, "client3", "client3@pharmacy.local",
            "Новиков Дмитрий Андреевич", "Client123!", RoleNames.Client);
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName, string email, string fullName, string password, string role)
    {
        // Если пользователь с таким логином уже есть, ничего не делаем
        if (await userManager.FindByNameAsync(userName) != null)
            return;

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            // Если пароль не прошёл правила, увидим понятную причину
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Не удалось создать пользователя {userName}: {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
    }

    // ---------- Категории, производители, поставщики, лекарства ----------
    private static async Task SeedCatalogAsync(ApplicationDbContext context)
    {
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Name = "Обезболивающие", Description = "Средства для снятия боли и снижения температуры" },
                new Category { Name = "Витамины", Description = "Витамины и минеральные комплексы" },
                new Category { Name = "Средства от простуды", Description = "Препараты при простуде и боли в горле" },
                new Category { Name = "Противовирусные", Description = "Препараты против вирусных инфекций" },
                new Category { Name = "Желудочно-кишечные препараты", Description = "Средства для пищеварения и ЖКТ" },
                new Category { Name = "Антибиотики", Description = "Антибактериальные препараты (по рецепту)" });
        }

        if (!await context.Manufacturers.AnyAsync())
        {
            context.Manufacturers.AddRange(
                new Manufacturer { Name = "Bayer", Country = "Германия", Description = "Международная фармацевтическая компания" },
                new Manufacturer { Name = "Фармстандарт", Country = "Россия", Description = "Российский производитель лекарств" },
                new Manufacturer { Name = "KRKA", Country = "Словения", Description = "Производитель дженериков" },
                new Manufacturer { Name = "Sanofi", Country = "Франция", Description = "Международная фармацевтическая компания" },
                new Manufacturer { Name = "Reckitt", Country = "Великобритания", Description = "Производитель товаров для здоровья" });
        }

        if (!await context.Suppliers.AnyAsync())
        {
            context.Suppliers.AddRange(
                new Supplier { Name = "ООО «Фарм-Опт»", Phone = "+7 495 123-45-67", Email = "info@farmopt.example", Address = "г. Москва, ул. Складская, 10" },
                new Supplier { Name = "АО «Медснаб»", Phone = "+7 812 765-43-21", Email = "sales@medsnab.example", Address = "г. Санкт-Петербург, пр. Лесной, 25" },
                new Supplier { Name = "ООО «Здоровье-Центр»", Phone = "+7 343 111-22-33", Email = "order@zdorovie.example", Address = "г. Екатеринбург, ул. Заводская, 7" });
        }

        await context.SaveChangesAsync();

        // Лекарства добавляем, только если таблица пуста
        if (await context.Medicines.AnyAsync())
            return;

        // Словари «название -> запись», чтобы получить Id по названию
        var categories = await context.Categories.ToDictionaryAsync(c => c.Name);
        var manufacturers = await context.Manufacturers.ToDictionaryAsync(m => m.Name);
        var suppliers = await context.Suppliers.ToDictionaryAsync(s => s.Name);

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Небольшая функция, чтобы не повторять один и тот же код 13 раз
        Medicine M(string name, string description, string category, string manufacturer,
                   string supplier, decimal price, int quantity, int monthsValid,
                   string barcode, bool prescription = false) => new()
                   {
                       Name = name,
                       Description = description,
                       CategoryId = categories[category].Id,
                       ManufacturerId = manufacturers[manufacturer].Id,
                       SupplierId = suppliers[supplier].Id,
                       Price = price,
                       Quantity = quantity,
                       ExpirationDate = today.AddMonths(monthsValid),
                       Barcode = barcode,
                       PrescriptionRequired = prescription,
                       IsActive = true
                   };

        const string optSupplier = "ООО «Фарм-Опт»";
        const string medSupplier = "АО «Медснаб»";
        const string healthSupplier = "ООО «Здоровье-Центр»";

        context.Medicines.AddRange(
            // Обезболивающие
            M("Парацетамол 500 мг", "Обезболивающее и жаропонижающее средство. Таблетки, 20 шт.",
              "Обезболивающие", "Фармстандарт", optSupplier, 50m, 120, 24, "4600000000011"),
            M("Нурофен 200 мг", "Обезболивающее и жаропонижающее средство. Таблетки, 24 шт.",
              "Обезболивающие", "Reckitt", medSupplier, 189m, 45, 18, "4600000000028"),
            M("Аспирин 500 мг", "Обезболивающее средство. Таблетки, 20 шт.",
              "Обезболивающие", "Bayer", medSupplier, 140m, 8, 12, "4600000000035"),

            // Витамины
            M("Витамин C 500 мг", "Аскорбиновая кислота. Таблетки, 30 шт.",
              "Витамины", "Фармстандарт", optSupplier, 80m, 200, 20, "4600000000042"),
            M("Витамин D3 2000 МЕ", "Витамин D. Капсулы, 60 шт.",
              "Витамины", "KRKA", healthSupplier, 320m, 60, 22, "4600000000059"),
            M("Магний B6", "Минеральный комплекс. Таблетки, 50 шт.",
              "Витамины", "Sanofi", medSupplier, 450m, 35, 15, "4600000000066"),

            // Средства от простуды
            M("Спрей для горла", "Антисептический спрей при боли в горле, 30 мл.",
              "Средства от простуды", "Фармстандарт", healthSupplier, 210m, 5, 14, "4600000000073"),
            M("Капли в нос", "Сосудосуживающие капли при насморке, 10 мл.",
              "Средства от простуды", "KRKA", optSupplier, 160m, 70, 16, "4600000000080"),

            // Противовирусные
            M("Арбидол", "Противовирусный препарат. Капсулы, 10 шт.",
              "Противовирусные", "Фармстандарт", optSupplier, 520m, 25, 19, "4600000000097"),
            M("Ацикловир 200 мг", "Противовирусный препарат. Таблетки, 20 шт.",
              "Противовирусные", "KRKA", medSupplier, 120m, 0, 10, "4600000000103"),

            // Желудочно-кишечные препараты
            M("Активированный уголь", "Сорбент. Таблетки, 10 шт.",
              "Желудочно-кишечные препараты", "Фармстандарт", optSupplier, 35m, 300, 30, "4600000000110"),
            M("Омепразол 20 мг", "Средство при повышенной кислотности. Капсулы, 28 шт.",
              "Желудочно-кишечные препараты", "KRKA", healthSupplier, 190m, 40, 1, "4600000000127"),

            // Антибиотики (только по рецепту)
            M("Амоксициллин 500 мг", "Антибиотик. Капсулы, 16 шт. Отпускается по рецепту.",
              "Антибиотики", "Sanofi", medSupplier, 230m, 30, 12, "4600000000134", prescription: true));

        await context.SaveChangesAsync();
    }
}