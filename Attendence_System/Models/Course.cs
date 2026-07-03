namespace Attendence_System.Models
{
    public class Course : IMustHaveTenant
    {
        public int CourseId { get; set; }
        public string Name { get; set; } = string.Empty;

        // Multi-tenancy
        public string TenantId { get; set; } = string.Empty;
        public Tenant? Tenant { get; set; }

        public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
        public ICollection<CourseGrade> CourseGrades { get; set; } = new List<CourseGrade>();
    }
}
