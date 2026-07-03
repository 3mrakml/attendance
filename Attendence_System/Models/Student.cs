using System.ComponentModel.DataAnnotations;

namespace Attendence_System.Models
{
    public class Student : IMustHaveTenant
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;

        [MaxLength(450)]
        public string QRToken { get; set; } = string.Empty;

        /// <summary>السن المُدخل يدوياً (قديم - محفوظ للتوافق مع البيانات القديمة)</summary>
        public int Age { get; set; }

        /// <summary>تاريخ الميلاد - يُستخدم لحساب السن الديناميكي</summary>
        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        // Multi-tenancy
        public string TenantId { get; set; } = string.Empty;
        public Tenant? Tenant { get; set; }

        public ICollection<StudentLecture>? StudentLectures { get; set; }

        public int GradeId { get; set; }
        public Grade? Grade { get; set; }

        /// <summary>يحسب السن بناءً على تاريخ الميلاد حتى تاريخ مُحدد، أو يرجع Age اليدوي لو مفيش تاريخ ميلاد</summary>
        public int CalculateAge(DateOnly referenceDate)
        {
            if (!DateOfBirth.HasValue) return Age;
            var dob = DateOfBirth.Value;
            int years = referenceDate.Year - dob.Year;
            if (referenceDate < dob.AddYears(years)) years--;
            return years;
        }
    }
}
