using Attendence_System.Models;
using Attendence_System.Services;
using Attendence_System.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Attendence_System.Filters;
using Attendence_System.Extensions;

namespace Attendence_System.Controllers
{
    [Authorize]
    [AutoClearStudentCache]
    public class LectureController : Controller
    {
        private readonly ILectureService _lectureService;
        private readonly ICourseService _courseService;
        private readonly IStudentService _studentService;
        private readonly IGradeService _gradeService;
        private readonly IExcelService _excelService;
        private readonly IMemoryCache _cache;

        public LectureController(
            ILectureService lectureService,
            ICourseService courseService,
            IStudentService studentService,
            IGradeService gradeService,
            IExcelService excelService,
            IMemoryCache cache)
        {
            _lectureService = lectureService;
            _courseService  = courseService;
            _studentService = studentService;
            _gradeService   = gradeService;
            _excelService   = excelService;
            _cache          = cache;
        }

        // ─── List Lectures (Index) ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(string search, int? gradeId)
        {
            ViewBag.CurrentSearch  = search;
            ViewBag.CurrentGradeId = gradeId;
            
            ViewBag.Grades = await _gradeService.GetAllGradesBasicAsync();
            var lectures = await _lectureService.GetFilteredLecturesAsync(search, gradeId);

            return View(lectures);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int lectureId, string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "اسم المحاضرة مطلوب.";
                return RedirectToAction(nameof(Index));
            }

            var updatedLecture = await _lectureService.UpdateLectureTitleAsync(lectureId, title);
            
            if (updatedLecture == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على المحاضرة.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "تم تعديل اسم المحاضرة بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        // ─── Create Lecture ────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var grades = await _gradeService.GetAllGradesAsync();

            var model = new LectureViewModel
            {
                Courses = new List<SelectListItem>(),
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
            if (model.GradeIds == null || !model.GradeIds.Any())
                ModelState.AddModelError("GradeIds", "يجب اختيار صف دراسي واحد على الأقل.");

            if (model.CourseId > 0 && model.GradeIds != null && model.GradeIds.Any())
            {
                var isAssigned = await _courseService.AreCoursesAssignedToGradesAsync(model.CourseId, model.GradeIds);
                if (!isAssigned)
                {
                    ModelState.AddModelError("GradeIds", "المادة غير مسجلة في جميع الصفوف المحددة.");
                }
            }

            if (!ModelState.IsValid)
            {
                var courses = await _courseService.GetAllCoursesAsync();
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

            try
            {
                var lecture = new Lecture
                {
                    Title = model.Title,
                    CourseId = model.CourseId,
                    DateTime = Attendence_System.Helpers.AppTime.Now
                };

                lecture = await _lectureService.CreateLectureAsync(lecture, model.GradeIds!);

                return RedirectToAction("Scan", new { id = lecture.LectureId });
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "حدث خطأ أثناء إنشاء المحاضرة. قد يكون بسبب عدم حفظ البيانات بشكل صحيح.");
                var courses = await _courseService.GetAllCoursesAsync();
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
        }

        [HttpPost]
        public async Task<IActionResult> CloseAttendance(int id)
        {
            await _lectureService.CloseAttendanceAsync(id);
            return RedirectToAction(nameof(Students), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetCoursesByGrades([FromQuery] List<int> gradeIds)
        {
            if (gradeIds == null || !gradeIds.Any())
                return Json(new List<object>());

            var courses = await _courseService.GetCommonCoursesByGradesAsync(gradeIds);

            var courseList = courses.Select(c => new { value = c.CourseId, text = c.Name }).ToList();
            return Json(courseList);
        }

        // ─── Lecture Students ──────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Students(int id, int page = 1)
        {
            var lecture = await _lectureService.GetLectureByIdAsync(id);
            if (lecture == null)
                return NotFound();

            var studentsStatus = await _lectureService.GetStudentAttendanceStatusForLectureAsync(id);

            var routeParams = new Dictionary<string, string> { { "id", id.ToString() } };
            var (paginatedStudents, paginationInfo) = studentsStatus.Paginate(page, 50, "Students", "Lecture", routeParams);

            var model = new LectureWithStudentsViewModel
            {
                lectureid = lecture.LectureId,
                LectureTitle = lecture.Title,
                TotalStudents = studentsStatus.Count,
                AttendedCount = studentsStatus.Count(s => s.IsAttended),
                AbsentCount = studentsStatus.Count(s => !s.IsAttended),
                Students = paginatedStudents
            };

            ViewBag.PaginationInfo = paginationInfo;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportLectureAttendance(int id)
        {
            var lecture = await _lectureService.GetLectureByIdAsync(id);
            if (lecture == null) return NotFound();

            var studentsStatus = await _lectureService.GetStudentAttendanceStatusForLectureAsync(id);

            var columns = new Dictionary<string, Func<StudentAttendanceStatus, object>>
            {
                { "الاسم بالكامل", s => s.Student.FullName },
                { "الصف", s => s.Student.Grade?.Name ?? "-" },
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

            ViewBag.LectureTitle = lecture.Title;
            ViewBag.LectureId = lecture.LectureId;
            ViewBag.IsClosed = lecture.IsAttendanceClosed;

            var attendedStudents = await _lectureService.GetStudentsInLectureAsync(id);
            ViewBag.AttendedStudents = attendedStudents.Select(s => s.FullName).ToList();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterAttendance([FromBody] ScanRequestVM request)
        {
            var result = await _studentService.RegisterAttendanceAsync(request);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> SyncCounts()
        {
            await _lectureService.SyncCountsAsync();
            return Ok("تمت المزامنة بنجاح");
        }
    }
}