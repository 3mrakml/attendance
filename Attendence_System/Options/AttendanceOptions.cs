using System.ComponentModel.DataAnnotations;

namespace Attendence_System.Options
{
    public class AttendanceOptions
    {
        public const string SectionName = "AttendanceSettings";

        [Required(ErrorMessage = " مسار رفع الملفات مطلوب ")]
        public string UploadFilesPath { get; set; } = "wwwroot/uploads";

        [Range(10, 600, ErrorMessage = " صلاحية الباركود يجب أن تكون بين 10 ثواني و 600 ثانية ")]
        public int QRCodeExpirySeconds { get; set; } = 60;
    }
}
