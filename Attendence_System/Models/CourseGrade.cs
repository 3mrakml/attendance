namespace Attendence_System.Models
{
    public class CourseGrade
    {
        public int CourseId { get; set; }
        public Course Course { get; set; }

        public int GradeId { get; set; }
        public Grade Grade { get; set; }
    }
}
