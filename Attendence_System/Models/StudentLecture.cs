namespace Attendence_System.Models
{
    public class StudentLecture : IMustHaveTenant
    {
        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }

        // Composite PK: (StudentId + LectureId) - defined in DbContext
        public int StudentId { get; set; }
        public Student Student { get; set; }

        public int LectureId { get; set; }
        public Lecture Lecture { get; set; }

        public DateTime AttendedAt { get; set; } = Attendence_System.Helpers.AppTime.Now; // وقت تسجيل الحضور
    }
}
