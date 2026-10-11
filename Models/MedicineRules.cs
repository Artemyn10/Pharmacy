namespace Pharmacy.Models;

// Общие правила для лекарств: подписи остатков и сроков годности
public static class MedicineRules
{
    // Если количество меньше этого числа, товар «заканчивается»
    public const int LowStockThreshold = 10;

    // За сколько дней до окончания срока считаем, что он «скоро истекает»
    public const int ExpiringSoonDays = 90;

    // ----- Остаток -----

    public static string GetStockLabel(int quantity) =>
        quantity <= 0 ? "Нет в наличии" :
        quantity < LowStockThreshold ? "Заканчивается" :
        "В наличии";

    public static string GetStockBadgeClass(int quantity) =>
        quantity <= 0 ? "bg-danger" :
        quantity < LowStockThreshold ? "bg-warning text-dark" :
        "bg-success";

    // ----- Срок годности -----

    // Пустая строка означает «со сроком всё в порядке»
    public static string GetExpirationLabel(DateOnly expirationDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (expirationDate < today)
            return "Срок истёк";

        if (expirationDate <= today.AddDays(ExpiringSoonDays))
            return "Скоро истекает";

        return string.Empty;
    }

    public static string GetExpirationBadgeClass(DateOnly expirationDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (expirationDate < today)
            return "bg-danger";

        if (expirationDate <= today.AddDays(ExpiringSoonDays))
            return "bg-warning text-dark";

        return string.Empty;
    }
}