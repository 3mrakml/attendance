namespace Attendence_System.Models
{
    public class StudentExam : IMustHaveTenant
    {
        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public int StudentExamId { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public int ExamId { get; set; }
        public Exam? Exam { get; set; }

        /// <summary>درجة الطالب في الامتحان (null = لم يُدخل بعد)</summary>
        public double? Score { get; set; }
    }
}
