namespace Attendence_System.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        public string Name { get; set; }

        public string UserId { get; set; }
        public AppUser User { get; set; }

        public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
        public ICollection<CourseGrade> CourseGrades { get; set; } = new List<CourseGrade>();

    }
}
