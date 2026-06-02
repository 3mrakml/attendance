using System.Threading.Tasks;
using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class AdminFormsController : Controller
    {
        private readonly ISystemSettingService _settingService;

        public AdminFormsController(ISystemSettingService settingService)
        {
            _settingService = settingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var isRegistrationOpenStr = await _settingService.GetSettingAsync("IsRegistrationOpen", "false");
            var registrationSuccessMessage = await _settingService.GetSettingAsync("RegistrationSuccessMessage", "تم التسجيل بنجاح! احتفظ بالباركود الخاص بك.");
            var whatsappGroupLink = await _settingService.GetSettingAsync("WhatsAppGroupLink", "");

            ViewBag.IsRegistrationOpen = isRegistrationOpenStr == "true";
            ViewBag.RegistrationSuccessMessage = registrationSuccessMessage;
            ViewBag.WhatsAppGroupLink = whatsappGroupLink;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(bool isRegistrationOpen, string registrationSuccessMessage, string whatsappGroupLink)
        {
            await _settingService.SetSettingAsync("IsRegistrationOpen", isRegistrationOpen ? "true" : "false");
            await _settingService.SetSettingAsync("RegistrationSuccessMessage", registrationSuccessMessage ?? "");
            await _settingService.SetSettingAsync("WhatsAppGroupLink", whatsappGroupLink ?? "");

            TempData["SuccessMessage"] = "تم تحديث إعدادات الفورم بنجاح.";
            return RedirectToAction("Index");
        }
    }
}
