using Attendence_System.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Attendence_System.Infrastructure
{
    public class HashidModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            // Get the injected HashidService
            var hashidService = bindingContext.HttpContext.RequestServices.GetRequiredService<IHashidService>();

            if (bindingContext.ModelType == typeof(int?))
            {
                var decoded = hashidService.DecodeNullable(value);
                if (decoded.HasValue)
                {
                    bindingContext.Result = ModelBindingResult.Success(decoded.Value);
                }
            }
            else if (bindingContext.ModelType == typeof(int))
            {
                var decoded = hashidService.Decode(value);
                if (decoded > 0)
                {
                    bindingContext.Result = ModelBindingResult.Success(decoded);
                }
            }

            return Task.CompletedTask;
        }
    }
}
