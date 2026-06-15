using Attendence_System.Models;
using Attendence_System.Services;
using Attendence_System.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IGradeService _gradeService;
        private readonly ISystemSettingService _settingService;
        private readonly IExcelService _excelService;
        private readonly IMemoryCache _cache;

        public ReportController(
            IStudentService studentService,
            IGradeService gradeService,
            ISystemSettingService settingService,
            IExcelService excelService,
            IMemoryCache cache)
        {
            _studentService = studentService;
            _gradeService = gradeService;
            _settingService = settingService;
            _excelService = excelService;
            _cache = cache;
        }

        private async Task<List<StudentReportItem>> GetCachedReportAsync(int? gradeId)
        {
            var tenantId = User.FindFirstValue("TenantId");
            string cacheKey = $"comprehensive_report_{tenantId}_{(gradeId.HasValue ? gradeId.Value.ToString() : "all")}";

            if (!_cache.TryGetValue(cacheKey, out List<StudentReportItem>? studentsReport) || studentsReport == null)
            {
                studentsReport = await _studentService.GetComprehensiveReportAsync(gradeId);
                var grades = await _gradeService.GetAllGradesAsync();

                var gradeSettings = new Dictionary<int, double>();
                foreach (var grade in grades)
                {
                    string key = $"Grade_{grade.GradeId}_Marks";
                    string valStr = await _settingService.GetSettingAsync(key, "10");
                    gradeSettings[grade.GradeId] = double.TryParse(valStr, out var parsed) ? parsed : 10;
                }

                foreach (var item in studentsReport)
                {
                    double maxMarks = gradeSettings.ContainsKey(item.GradeId) ? gradeSettings[item.GradeId] : 10;
                    item.CalculatedScore = Math.Round((item.AttendancePercentage / 100.0) * maxMarks, 2);
                }

                _cache.Set(cacheKey, studentsReport, TimeSpan.FromMinutes(5));
            }

            return studentsReport;
        }

        private List<StudentReportItem> SortReport(List<StudentReportItem> report, int? sortCol, bool sortAsc)
        {
            if (!sortCol.HasValue) return report;

            Func<StudentReportItem, object> keySelector = sortCol.Value switch
            {
                0 => s => s.FullName,
                1 => s => s.GradeName,
                2 => s => s.TotalLectures,
                3 => s => s.AttendedLectures,
                4 => s => s.AbsentLectures,
                5 => s => s.AttendancePercentage,
                6 => s => s.CalculatedScore,
                7 => s => s.ExamTotalScore,
                8 => s => s.TotalGrade,
                _ => s => s.FullName
            };

            return sortAsc ? report.OrderBy(keySelector).ToList() : report.OrderByDescending(keySelector).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? gradeId, int page = 1, int? sortCol = null, bool sortAsc = true)
        {
            var studentsReport = await GetCachedReportAsync(gradeId);
            studentsReport = SortReport(studentsReport, sortCol, sortAsc);

            int pageSize = 50;
            int totalItems = studentsReport.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var paginatedStudents = studentsReport.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var grades = await _gradeService.GetAllGradesAsync();
            var gradeSettings = new List<GradeMarksViewModel>();
            foreach (var grade in grades)
            {
                string key = $"Grade_{grade.GradeId}_Marks";
                string valStr = await _settingService.GetSettingAsync(key, "10");
                gradeSettings.Add(new GradeMarksViewModel
                {
                    GradeId = grade.GradeId,
                    GradeName = grade.Name,
                    MaxMarks = double.TryParse(valStr, out var parsed) ? parsed : 10
                });
            }

            var model = new ComprehensiveReportViewModel
            {
                GradeSettings = gradeSettings,
                Students = paginatedStudents,
                SelectedGradeId = gradeId
            };

            ViewBag.Grades = grades;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.SortCol = sortCol;
            ViewBag.SortAsc = sortAsc;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMarks(Dictionary<int, double> marks)
        {
            if (marks != null)
            {
                foreach (var mark in marks)
                {
                    string key = $"Grade_{mark.Key}_Marks";
                    await _settingService.SetSettingAsync(key, mark.Value.ToString());
                }
            }
            
            var tenantId = User.FindFirstValue("TenantId");
            if (!string.IsNullOrEmpty(tenantId))
            {
                // Clear report cache because scores changed
                var grades = await _gradeService.GetAllGradesAsync();
                _cache.Remove($"comprehensive_report_{tenantId}_all");
                foreach (var grade in grades)
                {
                    _cache.Remove($"comprehensive_report_{tenantId}_{grade.GradeId}");
                }
            }

            TempData["SuccessMessage"] = "تم تحديث إعدادات الدرجات بنجاح.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(int? gradeId, int? sortCol, bool sortAsc = true)
        {
            var studentsReport = await GetCachedReportAsync(gradeId);
            studentsReport = SortReport(studentsReport, sortCol, sortAsc);

            var columns = new Dictionary<string, Func<StudentReportItem, object>>
            {
                { "الاسم بالكامل", s => s.FullName },
                { "الصف/الفرقة", s => s.GradeName },
                { "إجمالي المحاضرات", s => s.TotalLectures },
                { "محاضرات الحضور", s => s.AttendedLectures },
                { "محاضرات الغياب", s => s.AbsentLectures },
                { "نسبة الحضور (%)", s => s.AttendancePercentage },
                { "درجة الحضور", s => s.CalculatedScore },
                { "درجات الامتحانات", s => s.ExamTotalScore },
                { "إجمالي درجات الامتحانات من", s => s.ExamMaxScore },
                { "الدرجة الكلية", s => s.TotalGrade }
            };

            var fileBytes = _excelService.ExportToExcel(studentsReport, "ComprehensiveReport", columns);

            string fileName = gradeId.HasValue
                ? $"Report_Grade_{gradeId.Value}_{DateTime.Now:yyyyMMdd}.xlsx"
                : $"ComprehensiveReport_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ─── Attendance-Only Report ────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Attendance(int? gradeId, int page = 1, int? sortCol = null, bool sortAsc = true)
        {
            var studentsReport = await GetCachedReportAsync(gradeId);
            studentsReport = SortReport(studentsReport, sortCol, sortAsc);

            int pageSize = 50;
            int totalItems = studentsReport.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var paginatedStudents = studentsReport.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Grades = await _gradeService.GetAllGradesAsync();
            ViewBag.SelectedGradeId = gradeId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.SortCol = sortCol;
            ViewBag.SortAsc = sortAsc;

            return View(paginatedStudents);
        }

        [HttpGet]
        public async Task<IActionResult> ExportAttendanceExcel(int? gradeId, int? sortCol, bool sortAsc = true)
        {
            var studentsReport = await GetCachedReportAsync(gradeId);
            studentsReport = SortReport(studentsReport, sortCol, sortAsc);

            var columns = new Dictionary<string, Func<StudentReportItem, object>>
            {
                { "الاسم بالكامل", s => s.FullName },
                { "رقم التليفون", s => s.PhoneNumber ?? "-" },
                { "الصف/الفرقة", s => s.GradeName },
                { "إجمالي المحاضرات", s => s.TotalLectures },
                { "محاضرات الحضور", s => s.AttendedLectures },
                { "محاضرات الغياب", s => s.AbsentLectures },
                { "نسبة الحضور (%)", s => s.AttendancePercentage }
            };

            var fileBytes = _excelService.ExportToExcel(studentsReport, "Attendance", columns);
            string fileName = gradeId.HasValue
                ? $"Attendance_Grade_{gradeId.Value}_{DateTime.Now:yyyyMMdd}.xlsx"
                : $"AttendanceReport_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
