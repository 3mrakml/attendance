using Attendence_System.Models;
using Attendence_System.Services;
using Attendence_System.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class CourseController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly ILectureService _lectureService;
        private readonly IStudentService _studentService;
        private readonly IGradeService _gradeService;
        private readonly IExcelService _excelService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CourseController> _logger;

        public CourseController(
            ICourseService courseService,
            ILectureService lectureService,
            IStudentService studentService,
            IGradeService gradeService,
            IExcelService excelService,
            UserManager<AppUser> userManager,
            ILogger<CourseController> logger)
        {
            _courseService = courseService;
            _lectureService = lectureService;
            _studentService = studentService;
            _gradeService = gradeService;
            _excelService = excelService;
            _userManager = userManager;
            _logger = logger;
        }

        // ─── Courses ───────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            var courses = await _courseService.GetCoursesByUserAsync(userId);
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
            var userId = _userManager.GetUserId(User);
            var newCourse = new Course
            {
                Name = model.Name,
                UserId = userId
            };

            await _courseService.CreateCourseAsync(newCourse, model.SelectedGradeIds);
            return RedirectToAction(nameof(Index));
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var userId = _userManager.GetUserId(User);
            var success = await _courseService.DeleteCourseAsync(id, userId);

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

            var userId = _userManager.GetUserId(User);
            if (course.UserId != userId)
                return Forbid();

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

            return Json(new { success = true, message = "Lecture deleted successfully" });
        }

        // ─── Attendance Statistics ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Attendance(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (course.UserId != userId)
                return Forbid();

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

            var userId = _userManager.GetUserId(User);
            if (course.UserId != userId) return Forbid();

            var studentsWithCount = await _studentService.GetCourseAttendanceStatsAsync(id);

            // Define custom columns
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
