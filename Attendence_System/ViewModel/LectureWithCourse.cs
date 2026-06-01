using Attendence_System.Models;

namespace Attendence_System.ViewModel
{
    public class LectureWithCourse
    {
        public List<Lecture> Lectures { get; set; }

        public string ? CouresName { get; set; }

        public int ? CourseId { get; set; }
    }
}
