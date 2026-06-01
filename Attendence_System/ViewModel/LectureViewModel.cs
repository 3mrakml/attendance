using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendence_System.ViewModel
{
    public class LectureViewModel
    {
        public int LectureId { get; set; }
        public string Title { get; set; }
        public string ?QRCode { get; set; }
        public int CourseId { get; set; } // لإضافة الكورس
        public IEnumerable<SelectListItem> ? Courses { get; set; } // قائمة الكورسات

        public int GradeId { get; set; } // الصف
        public IEnumerable<SelectListItem> ? Grades { get; set; } // قائمة الصفوف
    }
}
