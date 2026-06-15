using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Attendence_System.Filters
{
    public class AutoClearStudentCacheAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // If the action succeeded and it was a modifying request (POST, PUT, DELETE)
            if (context.Exception == null &&
                (context.HttpContext.Request.Method == "POST" ||
                 context.HttpContext.Request.Method == "DELETE" ||
                 context.HttpContext.Request.Method == "PUT"))
            {
                var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
                var tenantId = context.HttpContext.User.FindFirstValue("TenantId");
                if (cache != null && !string.IsNullOrEmpty(tenantId))
                {
                    cache.Remove($"students_index_{tenantId}");
                    cache.Remove($"attendance_perc_{tenantId}");
                    cache.Remove($"comprehensive_report_{tenantId}");
                }
            }
            base.OnActionExecuted(context);
        }
    }
}
