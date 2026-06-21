using Attendence_System.Models;
using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Attendence_System.Filters;
using Attendence_System.Extensions;

namespace Attendence_System.Controllers
{
    [Authorize]
    [AutoClearStudentCache]
    public class ExamController : Controller
    {
        private readonly IExamService _examService;
        private readonly ICourseService _courseService;
        private readonly IGradeService _gradeService;
        private readonly IExcelService _excelService;

        public ExamController(IExamService examService, ICourseService courseService, IGradeService gradeService, IExcelService excelService)
        {
            _examService = examService;
            _courseService = courseService;
            _gradeService = gradeService;
            _excelService = excelService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var exams = await _examService.GetAllExamsAsync();
            ViewBag.Courses = await _courseService.GetAllCoursesAsync();
            ViewBag.Grades = await _gradeService.GetAllGradesAsync();
            return View(exams);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Courses = await _courseService.GetAllCoursesAsync();
            ViewBag.Grades = await _gradeService.GetAllGradesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string title, DateOnly date, double maxScore, int? courseId, int gradeId)
        {
            var tenantId = User.FindFirstValue("TenantId");
            if (string.IsNullOrEmpty(tenantId))
                return RedirectToAction(nameof(Index));

            var exam = new Exam
            {
                Title = title,
                Date = date,
                MaxScore = maxScore,
                CourseId = courseId,
                GradeId = gradeId
            };

            await _examService.CreateExamAsync(exam, tenantId);
            TempData["SuccessMessage"] = "تم إنشاء الامتحان بنجاح.";
            return RedirectToAction(nameof(Scores), new { id = exam.ExamId });
        }

        [HttpGet]
        public async Task<IActionResult> Scores(int id, int page = 1)
        {
            var exam = await _examService.GetExamWithStudentsAsync(id);
            if (exam == null) return NotFound();

            var studentExams = await _examService.GetOrCreateStudentExamsAsync(id);
            
            var routeParams = new Dictionary<string, string> { { "id", id.ToString() } };
            var (paginatedExams, paginationInfo) = studentExams.Paginate(page, 50, "Scores", "Exam", routeParams);

            ViewBag.Exam = exam;
            ViewBag.PaginationInfo = paginationInfo;
            
            return View(paginatedExams);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScores(int examId, Dictionary<int, double?> scores, int page = 1)
        {
            await _examService.SaveStudentScoresAsync(examId, scores);
            TempData["SuccessMessage"] = "تم حفظ الدرجات بنجاح.";
            return RedirectToAction(nameof(Scores), new { id = examId, page = page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _examService.DeleteExamAsync(id);
            TempData["SuccessMessage"] = "تم حذف الامتحان بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        // ─── Results (Report) ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Results(int? courseId, int? gradeId, int? examId,
            string? search, string? sortCol, bool sortAsc = true, int page = 1)
        {
            ViewBag.Courses = await _courseService.GetAllCoursesAsync();
            ViewBag.Grades = await _gradeService.GetAllGradesAsync();

            var allExams = await _examService.GetAllExamsAsync();
            var filteredExams = allExams
                .Where(e => (!courseId.HasValue || e.CourseId == courseId)
                         && (!gradeId.HasValue || e.GradeId == gradeId))
                .ToList();
            ViewBag.Exams = filteredExams;
            ViewBag.SelectedCourseId = courseId;
            ViewBag.SelectedGradeId = gradeId;
            ViewBag.SelectedExamId = examId;
            ViewBag.Search = search;
            ViewBag.SortCol = sortCol;
            ViewBag.SortAsc = sortAsc;

            List<StudentExam> studentExams = new();
            Exam? selectedExam = null;

            if (examId.HasValue)
            {
                selectedExam = await _examService.GetExamWithStudentsAsync(examId.Value);
                if (selectedExam != null)
                {
                    studentExams = await _examService.GetOrCreateStudentExamsAsync(examId.Value);

                    // Search
                    if (!string.IsNullOrWhiteSpace(search))
                        studentExams = studentExams.Where(se =>
                            se.Student!.FullName.ContainsArabicFuzzy(search)).ToList();

                    // Sort
                    studentExams = (sortCol, sortAsc) switch
                    {
                        ("name", true)  => studentExams.OrderBy(se => se.Student!.FullName).ToList(),
                        ("name", false) => studentExams.OrderByDescending(se => se.Student!.FullName).ToList(),
                        ("score", true)  => studentExams.OrderBy(se => se.Score ?? -1).ToList(),
                        ("score", false) => studentExams.OrderByDescending(se => se.Score ?? -1).ToList(),
                        _ => studentExams.OrderByDescending(se => se.Score ?? -1).ToList()
                    };
                }
            }

            var routeParams = new Dictionary<string, string>
            {
                { "courseId", courseId?.ToString() },
                { "gradeId", gradeId?.ToString() },
                { "examId", examId?.ToString() },
                { "search", search },
                { "sortCol", sortCol },
                { "sortAsc", sortAsc.ToString().ToLower() }
            };

            var (paginatedExams, paginationInfo) = studentExams.Paginate(page, 50, "Results", "Exam", routeParams);

            ViewBag.PaginationInfo = paginationInfo;
            ViewBag.SelectedExam = selectedExam;
            
            return View(paginatedExams);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(int examId)
        {
            var exam = await _examService.GetExamWithStudentsAsync(examId);
            if (exam == null) return NotFound();

            var studentExams = await _examService.GetOrCreateStudentExamsAsync(examId);
            studentExams = studentExams.OrderByDescending(se => se.Score ?? -1).ToList();

            var columns = new Dictionary<string, Func<StudentExam, object>>
            {
                { "الاسم بالكامل", se => se.Student?.FullName ?? "" },
                { "الدرجة", se => se.Score.HasValue ? (object)se.Score.Value : "-" },
                { "الدرجة القصوى", se => exam.MaxScore },
                { "النسبة %", se => se.Score.HasValue && exam.MaxScore > 0
                    ? (object)Math.Round(se.Score.Value / exam.MaxScore * 100, 1)
                    : "-" },
                { "الحالة", se => {
                    if (!se.Score.HasValue) return "لم تُدخل";
                    double pct = exam.MaxScore > 0 ? se.Score.Value / exam.MaxScore * 100 : 0;
                    return pct >= 50 ? "ناجح" : "راسب";
                }}
            };

            var fileBytes = _excelService.ExportToExcel(studentExams, "ExamResults", columns);
            string fileName = $"امتحان_{exam.Title}_{exam.Grade?.Name}_{Attendence_System.Helpers.AppTime.Now:yyyyMMdd}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
