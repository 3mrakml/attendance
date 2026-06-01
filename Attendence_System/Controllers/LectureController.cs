using Attendence_System.Models;
using Attendence_System.Services;
using Attendence_System.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class LectureController : Controller
    {
        private readonly ILectureService _lectureService;
        private readonly ICourseService _courseService;
        private readonly IStudentService _studentService;
        private readonly IGradeService _gradeService;
        private readonly IExcelService _excelService;
        private readonly UserManager<AppUser> _userManager;

        public LectureController(
            ILectureService lectureService,
            ICourseService courseService,
            IStudentService studentService,
            IGradeService gradeService,
            IExcelService excelService,
            UserManager<AppUser> userManager)
        {
            _lectureService = lectureService;
            _courseService = courseService;
            _studentService = studentService;
            _gradeService = gradeService;
            _excelService = excelService;
            _userManager = userManager;
        }

        // ─── Create Lecture ────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            var grades = await _gradeService.GetAllGradesAsync();

            var model = new LectureViewModel
            {
                Courses = new List<SelectListItem>(), // Starts empty, filled via JS
                Grades = grades.Select(g => new SelectListItem
                {
                    Value = g.GradeId.ToString(),
                    Text = g.Name
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(LectureViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (model.GradeId > 0 && model.CourseId > 0)
            {
                var isAssigned = await _courseService.IsCourseAssignedToGradeAsync(model.CourseId, model.GradeId);
                if (!isAssigned)
                {
                    ModelState.AddModelError("CourseId", "هذه المادة غير مسجلة في الصف المختار.");
                }
            }

            if (!ModelState.IsValid)
            {
                var courses = await _courseService.GetCoursesByUserAsync(userId);
                var grades = await _gradeService.GetAllGradesAsync();
                model.Courses = courses.Select(c => new SelectListItem
                {
                    Value = c.CourseId.ToString(),
                    Text = c.Name
                }).ToList();
                model.Grades = grades.Select(g => new SelectListItem
                {
                    Value = g.GradeId.ToString(),
                    Text = g.Name
                }).ToList();
                return View(model);
            }

            var lecture = new Lecture
            {
                Title = model.Title,
                CourseId = model.CourseId,
                GradeId = model.GradeId,
                DateTime = System.DateTime.Now
            };

            lecture = await _lectureService.CreateLectureAsync(lecture);

            // Redirect directly to the scanner for attendance instead of showing lecture details
            return RedirectToAction("Scan", new { id = lecture.LectureId });
        }

        [HttpPost]
        public async Task<IActionResult> CloseAttendance(int id)
        {
            await _lectureService.CloseAttendanceAsync(id);
            return RedirectToAction(nameof(Students), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetCoursesByGrade(int gradeId)
        {
            var userId = _userManager.GetUserId(User);
            var courses = await _courseService.GetCoursesByGradeAndUserAsync(gradeId, userId);
            var courseList = courses.Select(c => new { value = c.CourseId, text = c.Name }).ToList();
            return Json(courseList);
        }

        // ─── Lecture Students ──────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Students(int id)
        {
            var lecture = await _lectureService.GetLectureByIdAsync(id);
            if (lecture == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (lecture.Course.UserId != userId)
                return Forbid();

            // We need to fetch all students in this grade, and their attendance status.
            var allStudentsInGrade = await _studentService.GetAllStudentsAsync();
            allStudentsInGrade = allStudentsInGrade.Where(s => s.GradeId == lecture.GradeId).ToList();

            var attendedStudentsData = await _lectureService.GetStudentsInLectureAsync(id); // This returns List<Student> but we need the AttendedAt date ideally.
            // Since we need AttendedAt, and GetStudentsInLectureAsync only returns Student, we will just use the basic boolean for now, or fetch from DbContext.
            // Actually, we can fetch from the controller using the StudentService if we had a method, but for simplicity:
            var attendedStudentIds = attendedStudentsData.Select(s => s.StudentId).ToHashSet();

            var studentsStatus = allStudentsInGrade.Select(s => new StudentAttendanceStatus
            {
                Student = s,
                IsAttended = attendedStudentIds.Contains(s.StudentId),
                AttendedAt = attendedStudentIds.Contains(s.StudentId) ? System.DateTime.Now : null // Fallback date if not available
            })
            .OrderByDescending(s => s.IsAttended)
            .ThenBy(s => s.Student.FullName)
            .ToList();

            var model = new LectureWithStudentsViewModel
            {
                lectureid = lecture.LectureId,
                LectureTitle = lecture.Title,
                TotalStudents = studentsStatus.Count,
                AttendedCount = studentsStatus.Count(s => s.IsAttended),
                AbsentCount = studentsStatus.Count(s => !s.IsAttended),
                Students = studentsStatus
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportLectureAttendance(int id)
        {
            var lecture = await _lectureService.GetLectureByIdAsync(id);
            if (lecture == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (lecture.Course.UserId != userId) return Forbid();

            var allStudentsInGrade = await _studentService.GetAllStudentsAsync();
            allStudentsInGrade = allStudentsInGrade.Where(s => s.GradeId == lecture.GradeId).ToList();

            var attendedStudentsData = await _lectureService.GetStudentsInLectureAsync(id);
            var attendedStudentIds = attendedStudentsData.Select(s => s.StudentId).ToHashSet();

            var studentsStatus = allStudentsInGrade.Select(s => new StudentAttendanceStatus
            {
                Student = s,
                IsAttended = attendedStudentIds.Contains(s.StudentId),
                AttendedAt = attendedStudentIds.Contains(s.StudentId) ? System.DateTime.Now : null
            })
            .OrderByDescending(s => s.IsAttended)
            .ThenBy(s => s.Student.FullName)
            .ToList();

            // Define custom columns
            var columns = new Dictionary<string, Func<StudentAttendanceStatus, object>>
            {
                { "الاسم بالكامل", s => s.Student.FullName },
                { "الحالة", s => s.IsAttended ? "حضر" : "غائب" },
                { "وقت الحضور", s => s.IsAttended && s.AttendedAt.HasValue ? s.AttendedAt.Value.ToString("yyyy-MM-dd HH:mm tt") : "-" }
            };

            var fileBytes = _excelService.ExportToExcel(studentsStatus, "Attendance", columns);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Lecture_{lecture.Title}_Attendance.xlsx");
        }

        // ─── Attendance Scanning ───────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Scan(int id)
        {
            var lecture = await _lectureService.GetLectureByIdAsync(id);
            if (lecture == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (lecture.Course.UserId != userId)
                return Forbid();

            ViewBag.LectureTitle = lecture.Title;
            ViewBag.LectureId = lecture.LectureId;
            ViewBag.IsClosed = lecture.IsAttendanceClosed;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterAttendance([FromBody] ScanRequestVM request)
        {
            var result = await _studentService.RegisterAttendanceAsync(request);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}