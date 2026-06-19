using Attendence_System.Models;
using Attendence_System.Services;
using Attendence_System.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Attendence_System.Filters;

namespace Attendence_System.Controllers
{
    [Authorize]
    [AutoClearStudentCache]
    public class CourseController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly ILectureService _lectureService;
        private readonly IStudentService _studentService;
        private readonly IGradeService _gradeService;
        private readonly IExcelService _excelService;
        private readonly IMemoryCache _cache;

        public CourseController(
            ICourseService courseService,
            ILectureService lectureService,
            IStudentService studentService,
            IGradeService gradeService,
            IExcelService excelService,
            IMemoryCache cache)
        {
            _courseService  = courseService;
            _lectureService = lectureService;
            _studentService = studentService;
            _gradeService   = gradeService;
            _excelService   = excelService;
            _cache          = cache;
        }

        // ─── Courses ───────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var courses = await _courseService.GetAllCoursesAsync();
            var availableGrades = await _gradeService.GetAllGradesAsync();

            var model = new CourseViewModel
            {
                Courses = courses,
                AvailableGrades = availableGrades
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CourseViewModel model)
        {
            var tenantId = User.FindFirstValue("TenantId");
            var newCourse = new Course
            {
                Name = model.Name,
                TenantId = tenantId!
            };

            await _courseService.CreateCourseAsync(newCourse, model.SelectedGradeIds);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int courseId, string name, List<int> selectedGradeIds)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("اسم المادة مطلوب");

            var updatedCourse = await _courseService.UpdateCourseAsync(courseId, name, selectedGradeIds);
            
            if (updatedCourse == null)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var success = await _courseService.DeleteCourseAsync(id);

            if (!success)
                return Json(new { success = false, message = "Course not found or access denied" });

            return Json(new { success = true, message = "Course deleted successfully" });
        }

        // ─── Lectures inside a Course ──────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ViewLectures(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            var lectures = await _lectureService.GetLecturesByCourseAsync(id);

            var model = new LectureWithCourse
            {
                Lectures = lectures.Select(l => new Lecture
                {
                    LectureId = l.LectureId,
                    Title = l.Title,
                    LectureGrades = l.LectureGrades,
                    IsAttendanceClosed = l.IsAttendanceClosed,
                    DateTime = l.DateTime,
                    CourseId = l.CourseId,
                    QRCode = l.QRCode
                }).ToList(),
                CouresName = course.Name,
                CourseId = course.CourseId,
            };

            return View(model);
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLecture(int id)
        {
            var success = await _lectureService.DeleteLectureAsync(id);
            if (!success)
                return Json(new { success = false, message = "Lecture not found" });

            // Invalidate lecture cache for the current tenant
            var tenantId = User.FindFirstValue("TenantId") ?? "";
            _cache.Remove($"lectures_all_{tenantId}");

            return Json(new { success = true, message = "Lecture deleted successfully" });
        }

        // ─── Attendance Statistics ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Attendance(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            var studentsWithCount = await _studentService.GetCourseAttendanceStatsAsync(id);

            var viewModel = new CourseWithStudentsViewModel
            {
                CourseId = course.CourseId,
                CourseTitle = course.Name,
                StudentsWithCount = studentsWithCount
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCourseAttendance(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null) return NotFound();

            var studentsWithCount = await _studentService.GetCourseAttendanceStatsAsync(id);

            var columns = new Dictionary<string, Func<StudentWithCount, object>>
            {
                { "الاسم بالكامل", s => s.FullName },
                { "رقم QR", s => s.QRToken },
                { "السن", s => s.Age },
                { "عدد مرات الحضور", s => s.Count }
            };

            var fileBytes = _excelService.ExportToExcel(studentsWithCount, "Attendance", columns);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Course_{course.Name}_Attendance.xlsx");
        }
    }
}
