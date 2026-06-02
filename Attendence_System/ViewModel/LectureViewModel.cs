using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendence_System.ViewModel
{
    public class LectureViewModel
    {
        public int LectureId { get; set; }
        public string Title { get; set; }
        public string? QRCode { get; set; }

        public int CourseId { get; set; }
        public IEnumerable<SelectListItem>? Courses { get; set; }

        // Many-to-Many: محاضرة لأكثر من صف/مستوى
        public List<int> GradeIds { get; set; } = new List<int>();
        public IEnumerable<SelectListItem>? Grades { get; set; }
    }
}
