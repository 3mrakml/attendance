using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Attendence_System.Services;
using Attendence_System.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IGradeService _gradeService;
        private readonly ISystemSettingService _settingService;
        private readonly IExcelService _excelService;

        public ReportController(
            IStudentService studentService, 
            IGradeService gradeService, 
            ISystemSettingService settingService,
            IExcelService excelService)
        {
            _studentService = studentService;
            _gradeService = gradeService;
            _settingService = settingService;
            _excelService = excelService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? gradeId)
        {
            var studentsReport = await _studentService.GetComprehensiveReportAsync(gradeId);
            var grades = await _gradeService.GetAllGradesAsync();

            var gradeSettings = new List<GradeMarksViewModel>();
            foreach (var grade in grades)
            {
                string key = $"Grade_{grade.GradeId}_Marks";
                string valStr = await _settingService.GetSettingAsync(key, "10");
                double val = double.TryParse(valStr, out var parsed) ? parsed : 10;

                gradeSettings.Add(new GradeMarksViewModel
                {
                    GradeId = grade.GradeId,
                    GradeName = grade.Name,
                    MaxMarks = val
                });
            }

            // Calculate final scores
            foreach (var item in studentsReport)
            {
                var settings = gradeSettings.FirstOrDefault(g => g.GradeId == item.GradeId);
                double maxMarks = settings?.MaxMarks ?? 10;
                item.CalculatedScore = Math.Round((item.AttendancePercentage / 100.0) * maxMarks, 2);
            }

            var model = new ComprehensiveReportViewModel
            {
                GradeSettings = gradeSettings,
                Students = studentsReport,
                SelectedGradeId = gradeId
            };

            ViewBag.Grades = grades;

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

            TempData["SuccessMessage"] = "تم تحديث إعدادات الدرجات بنجاح.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(int? gradeId, int? sortCol, bool sortAsc = true)
        {
            var studentsReport = await _studentService.GetComprehensiveReportAsync(gradeId);
            var grades = await _gradeService.GetAllGradesAsync();

            var gradeSettings = new Dictionary<int, double>();
            foreach (var grade in grades)
            {
                string key = $"Grade_{grade.GradeId}_Marks";
                string valStr = await _settingService.GetSettingAsync(key, "10");
                gradeSettings[grade.GradeId] = double.TryParse(valStr, out var parsed) ? parsed : 10;
            }

            // Calculate final scores
            foreach (var item in studentsReport)
            {
                double maxMarks = gradeSettings.ContainsKey(item.GradeId) ? gradeSettings[item.GradeId] : 10;
                item.CalculatedScore = Math.Round((item.AttendancePercentage / 100.0) * maxMarks, 2);
            }

            // Apply Sorting
            if (sortCol.HasValue)
            {
                Func<StudentReportItem, object> keySelector = sortCol.Value switch
                {
                    0 => s => s.FullName,
                    1 => s => s.GradeName,
                    2 => s => s.TotalLectures,
                    3 => s => s.AttendedLectures,
                    4 => s => s.AbsentLectures,
                    5 => s => s.AttendancePercentage,
                    6 => s => s.CalculatedScore,
                    _ => s => s.FullName
                };

                studentsReport = sortAsc 
                    ? studentsReport.OrderBy(keySelector).ToList() 
                    : studentsReport.OrderByDescending(keySelector).ToList();
            }

            var columns = new Dictionary<string, Func<StudentReportItem, object>>
            {
                { "الاسم بالكامل", s => s.FullName },
                { "الصف/الفرقة", s => s.GradeName },
                { "إجمالي المحاضرات", s => s.TotalLectures },
                { "محاضرات الحضور", s => s.AttendedLectures },
                { "محاضرات الغياب", s => s.AbsentLectures },
                { "نسبة الحضور (%)", s => s.AttendancePercentage },
                { "الدرجة المستحقة", s => s.CalculatedScore }
            };

            var fileBytes = _excelService.ExportToExcel(studentsReport, "ComprehensiveReport", columns);
            
            string fileName = gradeId.HasValue 
                ? $"Report_Grade_{gradeId.Value}_{DateTime.Now:yyyyMMdd}.xlsx" 
                : $"ComprehensiveReport_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
