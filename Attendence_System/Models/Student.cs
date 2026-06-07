using System.ComponentModel.DataAnnotations;

namespace Attendence_System.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;

        [MaxLength(450)]
        public string QRToken { get; set; } = string.Empty;
        public int Age { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        // Multi-tenancy
        public string TenantId { get; set; } = string.Empty;
        public Tenant? Tenant { get; set; }

        public ICollection<StudentLecture>? StudentLectures { get; set; }

        public int GradeId { get; set; }
        public Grade? Grade { get; set; }
    }
}
