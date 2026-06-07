using System.ComponentModel.DataAnnotations;

namespace Attendence_System.Models
{
    public class SystemSetting
    {
        public string TenantId { get; set; } = string.Empty;
        public Tenant? Tenant { get; set; }

        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
