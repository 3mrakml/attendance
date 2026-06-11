namespace Attendence_System.Models
{
    public class StudentExam
    {
        public int StudentExamId { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public int ExamId { get; set; }
        public Exam? Exam { get; set; }

        /// <summary>درجة الطالب في الامتحان (null = لم يُدخل بعد)</summary>
        public double? Score { get; set; }
    }
}
