namespace Attendence_System.Models
{
    public class Grade : IMustHaveTenant
    {
        public int GradeId { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public int Code { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        
        public int StudentCount { get; set; } // Cached total students in this grade

        // Multi-tenancy
        public string TenantId { get; set; } = string.Empty;
        public Tenant? Tenant { get; set; }

        // العلاقات
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<CourseGrade> CourseGrades { get; set; } = new List<CourseGrade>();
        public ICollection<LectureGrade> LectureGrades { get; set; } = new List<LectureGrade>();
    }
}
