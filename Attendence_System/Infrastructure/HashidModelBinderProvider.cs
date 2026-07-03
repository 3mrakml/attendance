using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Attendence_System.Infrastructure
{
    public class HashidModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Metadata.ModelType == typeof(int) || context.Metadata.ModelType == typeof(int?))
            {
                var name = context.Metadata.Name ?? context.Metadata.PropertyName;
                if (!string.IsNullOrEmpty(name) && 
                    (name.Equals("id", StringComparison.OrdinalIgnoreCase) || 
                     name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)))
                {
                    return new HashidModelBinder();
                }
            }

            return null;
        }
    }
}
