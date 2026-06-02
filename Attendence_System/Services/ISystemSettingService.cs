using System.Threading.Tasks;

namespace Attendence_System.Services
{
    public interface ISystemSettingService
    {
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task SetSettingAsync(string key, string value);
    }
}
