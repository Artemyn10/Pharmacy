using System.Globalization;
using System.Text;
using Pharmacy.Models.ViewModels;

namespace Pharmacy.Services;

public static class ReceiptTextBuilder
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static string ForSale(SaleDetailsViewModel sale)
    {
        var text = new StringBuilder();

        text.AppendLine($"ЧЕК ПРОДАЖИ №{sale.Id}");
        text.AppendLine($"Дата и время: {sale.SaleDate.ToLocalTime():dd.MM.yyyy HH:mm}");
        text.AppendLine($"Фармацевт: {sale.UserFullName}");
        text.AppendLine(new string('-', 40));
        text.AppendLine("Товары:");

        foreach (var item in sale.Items)
        {
            text.AppendLine(item.MedicineName);
            text.AppendLine(
                $"  {item.Quantity} шт. x {Money(item.Price)} = {Money(item.TotalPrice)}");
        }

        text.AppendLine(new string('-', 40));
        text.AppendLine($"ИТОГО: {Money(sale.TotalAmount)}");
        text.AppendLine($"Статус: {sale.Status}");

        return text.ToString();
    }

    public static string ForCustomerOrder(CustomerOrderDetailsViewModel order)
    {
        var text = new StringBuilder();

        text.AppendLine($"ЧЕК ЗАКАЗА №{order.Id}");
        text.AppendLine($"Дата и время: {order.OrderDate.ToLocalTime():dd.MM.yyyy HH:mm}");
        text.AppendLine($"Клиент: {order.CustomerName}");
        text.AppendLine(new string('-', 40));
        text.AppendLine("Товары:");

        foreach (var item in order.Items)
        {
            text.AppendLine(item.MedicineName);
            text.AppendLine(
                $"  {item.Quantity} шт. x {Money(item.UnitPrice)} = {Money(item.TotalPrice)}");
        }

        text.AppendLine(new string('-', 40));
        text.AppendLine($"ИТОГО: {Money(order.TotalAmount)}");
        text.AppendLine($"Статус: {order.Status}");

        return text.ToString();
    }

    private static string Money(decimal amount) =>
        $"{amount.ToString("N2", RussianCulture)} руб.";
}