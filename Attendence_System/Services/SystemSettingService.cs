using Attendence_System.Data;
using Attendence_System.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Attendence_System.Services
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SystemSettingService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            // Global Query Filter automatically scopes to current tenant
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value ?? defaultValue;
        }

        public async Task SetSettingAsync(string key, string value)
        {
            var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("TenantId");
            if (string.IsNullOrEmpty(tenantId)) throw new Exception("TenantId is missing from current user claims.");

            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting != null)
            {
                setting.Value = value;
                _context.SystemSettings.Update(setting);
            }
            else
            {
                _context.SystemSettings.Add(new SystemSetting { Key = key, Value = value, TenantId = tenantId });
            }
            await _context.SaveChangesAsync();
        }
    }
}
