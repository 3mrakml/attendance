namespace Attendence_System.Models
{
    public class Grade
    {
        public int GradeId { get; set; }
        public string Name { get; set; } // اسم الصف: الفرقة الأولى، الفرقة الثانية، الخ

        // العلاقات
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<CourseGrade> CourseGrades { get; set; } = new List<CourseGrade>();
        public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
    }
}
