using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class SettingController : Controller
    {
        private readonly ISystemSettingService _settingService;
        private readonly IGradeService _gradeService;

        public SettingController(ISystemSettingService settingService, IGradeService gradeService)
        {
            _settingService = settingService;
            _gradeService = gradeService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.AgeReferenceDate = await _settingService.GetSettingAsync("AgeReferenceDate");
            ViewBag.CodeType = await _settingService.GetSettingAsync("CodeType", "QR");

            var grades = await _gradeService.GetAllGradesAsync();
            var gradeSettings = new Dictionary<int, double>();
            foreach (var grade in grades)
            {
                string key = $"Grade_{grade.GradeId}_Marks";
                string valStr = await _settingService.GetSettingAsync(key, "10");
                gradeSettings[grade.GradeId] = double.TryParse(valStr, out var parsed) ? parsed : 10;
            }
            ViewBag.Grades = grades;
            ViewBag.GradeSettings = gradeSettings;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(string ageReferenceDate, string codeType, Dictionary<int, double> marks)
        {
            await _settingService.SetSettingAsync("AgeReferenceDate", ageReferenceDate);
            await _settingService.SetSettingAsync("CodeType", string.IsNullOrEmpty(codeType) ? "QR" : codeType);
            
            if (marks != null)
            {
                foreach (var mark in marks)
                {
                    string key = $"Grade_{mark.Key}_Marks";
                    await _settingService.SetSettingAsync(key, mark.Value.ToString());
                }
            }
            
            TempData["SuccessMessage"] = "تم حفظ الإعدادات بنجاح.";
            return RedirectToAction(nameof(Index));
        }
    }
}
