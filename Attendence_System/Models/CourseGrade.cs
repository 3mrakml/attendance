namespace Attendence_System.Models
{
    public class CourseGrade : IMustHaveTenant
    {
        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public int GradeId { get; set; }
        public Grade Grade { get; set; }
    }
}
