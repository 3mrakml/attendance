namespace Attendence_System.Models
{
    public class Lecture : IMustHaveTenant
    {
        public int LectureId { get; set; }

        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }
        public string Title { get; set; }            // عنوان المحاضرة
        public DateTime DateTime { get; set; }       // تاريخ ووقت المحاضرة
        public string? QRCode { get; set; }          // مسار صورة QR Code للمحاضرة
        public bool IsAttendanceClosed { get; set; } // هل أُغلق باب تسجيل الحضور
        
        public int AttendedCount { get; set; } // Cached count of attended students

        public int CourseId { get; set; }
        public Course Course { get; set; }

        // علاقة Many-to-Many مع الصفوف الدراسية (محاضرة لأكثر من مستوى)
        public ICollection<LectureGrade> LectureGrades { get; set; } = new List<LectureGrade>();

        public ICollection<StudentLecture> StudentLectures { get; set; }
    }
}
