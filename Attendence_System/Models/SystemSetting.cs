using System.ComponentModel.DataAnnotations;

namespace Attendence_System.Models
{
    public class SystemSetting
    {
        [Key]
        [MaxLength(100)]
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
