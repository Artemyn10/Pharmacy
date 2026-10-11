namespace Pharmacy.Models;

// Названия ролей в одном месте.
// Дальше будем писать [Authorize(Roles = RoleNames.Administrator)]
public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string Pharmacist = "Pharmacist";
    public const string Client = "Client";

    // Для действий, доступных и админу, и фармацевту
    public const string AdminOrPharmacist = Administrator + "," + Pharmacist;

    // Русское название роли для показа пользователю
    public static string GetDisplayName(string? role) => role switch
    {
        Administrator => "Администратор",
        Pharmacist => "Фармацевт",
        Client => "Клиент",
        _ => "—"
    };
}