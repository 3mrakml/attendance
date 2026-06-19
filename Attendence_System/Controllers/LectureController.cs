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
        public async Task<IActionResult> Index(string search, int? gradeId, int page = 1)
        {
            ViewBag.CurrentSearch  = search;
            ViewBag.CurrentGradeId = gradeId;
            ViewBag.Grades = await _gradeService.GetAllGradesBasicAsync();

            int pageSize = 20;

            // ── Cache all lectures for 10 minutes to avoid reloading on every request ──
            var tenantId = User.FindFirstValue("TenantId") ?? "";
            string cacheKey = $"lectures_all_{tenantId}";

            if (!_cache.TryGetValue(cacheKey, out List<Lecture>? allLectures) || allLectures == null)
            {
                allLectures = await _lectureService.GetAllLecturesQueryable().ToListAsync();
                _cache.Set(cacheKey, allLectures, TimeSpan.FromMinutes(10));
            }

            IEnumerable<Lecture> filteredLectures = allLectures;

            if (!string.IsNullOrWhiteSpace(search))
            {
                filteredLectures = filteredLectures.Where(l =>
                    l.Title.ContainsArabicFuzzy(search) ||
                    (l.Course != null && l.Course.Name.ContainsArabicFuzzy(search)));
            }

            if (gradeId.HasValue)
            {
                filteredLectures = filteredLectures.Where(l =>
                    l.LectureGrades.Any(lg => lg.GradeId == gradeId.Value));
            }

            int totalCount  = filteredLectures.Count();
            var pagedItems  = filteredLectures
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var paginatedLectures = new PaginatedList<Lecture>(pagedItems, totalCount, page, pageSize);

            // Fetch student counts + attended counts sequentially to avoid EF Core concurrency issues on the same DbContext
            var lectureIds = pagedItems.Select(l => l.LectureId).ToList();
            var studentCountByGrade = await _studentService.GetStudentCountByGradeAsync();
            var attendedCounts = await _lectureService.GetAttendedCountsForLecturesAsync(lectureIds);

            ViewBag.StudentCountByGrade = studentCountByGrade;
            ViewBag.AttendedCounts      = attendedCounts;

            return View(paginatedLectures);
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

            InvalidateLectureCache();
            TempData["SuccessMessage"] = "تم تعديل اسم المحاضرة بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        private void InvalidateLectureCache()
        {
            var tenantId = User.FindFirstValue("TenantId") ?? "";
            _cache.Remove($"lectures_all_{tenantId}");
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
                foreach (var gradeId in model.GradeIds)
                {
                    var isAssigned = await _courseService.IsCourseAssignedToGradeAsync(model.CourseId, gradeId);
                    if (!isAssigned)
                    {
                        var grades = await _gradeService.GetAllGradesAsync();
                        var gradeName = grades.FirstOrDefault(g => g.GradeId == gradeId)?.Name ?? gradeId.ToString();
                        ModelState.AddModelError("GradeIds", $"المادة غير مسجلة في الصف: {gradeName}.");
                        break;
                    }
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

            var lecture = new Lecture
            {
                Title = model.Title,
                CourseId = model.CourseId,
                DateTime = System.DateTime.Now
            };

            lecture = await _lectureService.CreateLectureAsync(lecture, model.GradeIds!);

            InvalidateLectureCache();
            return RedirectToAction("Scan", new { id = lecture.LectureId });
        }

        [HttpPost]
        public async Task<IActionResult> CloseAttendance(int id)
        {
            await _lectureService.CloseAttendanceAsync(id);
            InvalidateLectureCache();
            return RedirectToAction(nameof(Students), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetCoursesByGrades([FromQuery] List<int> gradeIds)
        {
            if (gradeIds == null || !gradeIds.Any())
                return Json(new List<object>());

            var courses = await _courseService.GetCoursesByGradeAsync(gradeIds[0]);

            for (int i = 1; i < gradeIds.Count; i++)
            {
                var nextGradeCourses = await _courseService.GetCoursesByGradeAsync(gradeIds[i]);
                var nextGradeCourseIds = nextGradeCourses.Select(c => c.CourseId).ToHashSet();
                courses = courses.Where(c => nextGradeCourseIds.Contains(c.CourseId)).ToList();
            }

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

            var lectureGradeIds = lecture.LectureGrades.Select(lg => lg.GradeId).ToHashSet();
            var allStudents = await _studentService.GetAllStudentsAsync();
            var allStudentsInGrades = allStudents.Where(s => lectureGradeIds.Contains(s.GradeId)).ToList();

            var attendedStudentsData = await _lectureService.GetStudentsInLectureAsync(id);
            var attendedStudentIds = attendedStudentsData.Select(s => s.StudentId).ToHashSet();

            var studentsStatus = allStudentsInGrades.Select(s => new StudentAttendanceStatus
            {
                Student = s,
                IsAttended = attendedStudentIds.Contains(s.StudentId),
                AttendedAt = attendedStudentIds.Contains(s.StudentId) ? System.DateTime.Now : null
            })
            .OrderByDescending(s => s.IsAttended)
            .ThenBy(s => s.Student.FullName)
            .ToList();

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

            var lectureGradeIds = lecture.LectureGrades.Select(lg => lg.GradeId).ToHashSet();
            var allStudents = await _studentService.GetAllStudentsAsync();
            var allStudentsInGrades = allStudents.Where(s => lectureGradeIds.Contains(s.GradeId)).ToList();

            var attendedStudentsData = await _lectureService.GetStudentsInLectureAsync(id);
            var attendedStudentIds = attendedStudentsData.Select(s => s.StudentId).ToHashSet();

            var studentsStatus = allStudentsInGrades.Select(s => new StudentAttendanceStatus
            {
                Student = s,
                IsAttended = attendedStudentIds.Contains(s.StudentId),
                AttendedAt = attendedStudentIds.Contains(s.StudentId) ? System.DateTime.Now : null
            })
            .OrderByDescending(s => s.IsAttended)
            .ThenBy(s => s.Student.FullName)
            .ToList();

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
    }
}