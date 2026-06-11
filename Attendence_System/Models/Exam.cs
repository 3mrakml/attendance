namespace Attendence_System.Models
{
    public class Exam
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public double MaxScore { get; set; } = 100;

        // المادة الدراسية
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        // الصف الدراسي
        public int GradeId { get; set; }
        public Grade? Grade { get; set; }

        // Multi-tenancy
        public string TenantId { get; set; } = string.Empty;
        public Tenant? Tenant { get; set; }

        public ICollection<StudentExam> StudentExams { get; set; } = new List<StudentExam>();
    }
}
