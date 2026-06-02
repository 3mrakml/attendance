using System.ComponentModel.DataAnnotations;

namespace Attendence_System.Models
{
    public class Student
    {
        public int StudentId { get; set; }         // معرف الطالب (PK)
        public string FullName { get; set; }        // الاسم الكامل
        [MaxLength(450)]
        public string QRToken { get; set; }         // رمز QR الفريد (auto-generated)
        public int Age { get; set; }                // السن

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }    // رقم هاتف الطالب

        public ICollection<StudentLecture>? StudentLectures { get; set; }

        public int GradeId { get; set; }
        public Grade? Grade { get; set; }
    }
}
