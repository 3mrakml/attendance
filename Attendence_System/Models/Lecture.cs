namespace Attendence_System.Models
{
    public class Lecture
    {
        public int LectureId { get; set; }          // معرف المحاضرة (PK)
        public string Title { get; set; }            // عنوان المحاضرة
        public DateTime DateTime { get; set; }       // تاريخ ووقت المحاضرة
        public string? QRCode { get; set; }          // مسار صورة QR Code للمحاضرة
        public bool IsAttendanceClosed { get; set; } // هل أُغلق باب تسجيل الحضور

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public int GradeId { get; set; }             // الصف الدراسي المخصص (NOT NULL)
        public Grade Grade { get; set; }

        public ICollection<StudentLecture> StudentLectures { get; set; }
    }
}
