using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Pharmacy.Services;

// Говорит ASP.NET Core: «для полей типа decimal используй наш переводчик»
public class DecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.UnderlyingOrModelType == typeof(decimal))
            return new DecimalModelBinder();

        return null;
    }
}

// Сам «переводчик»: принимает и 50.5, и 50,5
public class DecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask;

        // Запоминаем введённое значение, чтобы показать его в форме при ошибке
        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

        var text = valueResult.FirstValue;
        var field = bindingContext.ModelMetadata.DisplayName ?? bindingContext.ModelName;

        if (string.IsNullOrWhiteSpace(text))
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName, $"Поле «{field}» обязательно для заполнения.");
            return Task.CompletedTask;
        }

        // Убираем пробелы (в том числе неразрывные) и заменяем запятую на точку
        text = text.Replace(" ", "").Replace("\u00A0", "").Replace(',', '.');

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName, $"Поле «{field}» должно быть числом.");
        }

        return Task.CompletedTask;
    }
}