using System.Threading.Tasks;
using Attendence_System.Data;
using Attendence_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Attendence_System.Services
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly ApplicationDbContext _context;

        public SystemSettingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting != null ? setting.Value : defaultValue;
        }

        public async Task SetSettingAsync(string key, string value)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new SystemSetting { Key = key, Value = value };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                _context.SystemSettings.Update(setting);
            }
            await _context.SaveChangesAsync();
        }
    }
}
