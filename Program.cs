using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;
using Pharmacy.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Строка подключения 'DefaultConnection' не найдена.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Правила пароля
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;

    // Email должен быть уникальным
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddErrorDescriber<RussianIdentityErrorDescriber>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
builder.Services.AddScoped<MedicineListService>();
builder.Services.AddControllersWithViews(options =>
{
    // Наш «переводчик» для чисел decimal (понимает и точку, и запятую)
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());

    // Русские тексты стандартных ошибок привязки данных формы
    var messages = options.ModelBindingMessageProvider;
    messages.SetValueMustNotBeNullAccessor(_ => "Поле обязательно для заполнения.");
    messages.SetMissingBindRequiredValueAccessor(field => $"Не указано значение поля «{field}».");
    messages.SetMissingKeyOrValueAccessor(() => "Необходимо указать значение.");
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"Значение «{value}» недопустимо для поля «{field}».");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Значение «{value}» недопустимо.");
    messages.SetUnknownValueIsInvalidAccessor(field => $"Недопустимое значение поля «{field}».");
    messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Недопустимое значение.");
    messages.SetValueIsInvalidAccessor(value => $"Значение «{value}» недопустимо.");
    messages.SetValueMustBeANumberAccessor(field => $"Поле «{field}» должно быть числом.");
    messages.SetNonPropertyValueMustBeANumberAccessor(() => "Значение должно быть числом.");
});

var app = builder.Build();

// При каждом запуске: применяем миграции и добавляем начальные данные
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Русская культура для всего сайта
var russianCulture = new CultureInfo("ru-RU");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(russianCulture),
    SupportedCultures = new[] { russianCulture },
    SupportedUICultures = new[] { russianCulture }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();