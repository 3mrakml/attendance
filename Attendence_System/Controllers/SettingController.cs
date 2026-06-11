using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class SettingController : Controller
    {
        private readonly ISystemSettingService _settingService;

        public SettingController(ISystemSettingService settingService)
        {
            _settingService = settingService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.AgeReferenceDate = await _settingService.GetSettingAsync("AgeReferenceDate");
            ViewBag.CodeType = await _settingService.GetSettingAsync("CodeType", "QR");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(string ageReferenceDate, string codeType)
        {
            await _settingService.SetSettingAsync("AgeReferenceDate", ageReferenceDate);
            await _settingService.SetSettingAsync("CodeType", string.IsNullOrEmpty(codeType) ? "QR" : codeType);
            
            TempData["SuccessMessage"] = "تم حفظ الإعدادات بنجاح.";
            return RedirectToAction(nameof(Index));
        }
    }
}
