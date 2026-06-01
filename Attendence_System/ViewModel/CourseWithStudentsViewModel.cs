using Attendence_System.Models;

namespace Attendence_System.ViewModel
{
    public class CourseWithStudentsViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }  // عنوان المحاضرة
        public List<StudentWithCount> StudentsWithCount { get; set; } // إضافة خاصية الطلاب مع عدد التكرارات
    }
}
