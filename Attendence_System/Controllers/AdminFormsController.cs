using Attendence_System.Data;
using Attendence_System.Models;
using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class AdminFormsController : Controller
    {
        private readonly ISystemSettingService _settingService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminFormsController(
            ISystemSettingService settingService,
            UserManager<AppUser> userManager,
            ApplicationDbContext context)
        {
            _settingService = settingService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tenantId = User.FindFirstValue("TenantId");

            var isRegistrationOpenStr = await _settingService.GetSettingAsync("IsRegistrationOpen", "false");
            var registrationSuccessMessage = await _settingService.GetSettingAsync("RegistrationSuccessMessage", "تم التسجيل بنجاح! احتفظ بالباركود الخاص بك.");
            var whatsappGroupLink = await _settingService.GetSettingAsync("WhatsAppGroupLink", "");
            var ageReferenceDate = await _settingService.GetSettingAsync("AgeReferenceDate", "");

            var isGradeQueryOpenStr = await _settingService.GetSettingAsync("IsGradeQueryOpen", "false");

            ViewBag.IsRegistrationOpen = isRegistrationOpenStr == "true";
            ViewBag.IsGradeQueryOpen = isGradeQueryOpenStr == "true";
            ViewBag.RegistrationSuccessMessage = registrationSuccessMessage;
            ViewBag.WhatsAppGroupLink = whatsappGroupLink;
            ViewBag.AgeReferenceDate = ageReferenceDate;
            ViewBag.TenantId = tenantId;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GradesQuery()
        {
            var tenantId = User.FindFirstValue("TenantId");

            var isGradeQueryOpenStr = await _settingService.GetSettingAsync("IsGradeQueryOpen", "false");

            ViewBag.IsGradeQueryOpen = isGradeQueryOpenStr == "true";
            ViewBag.TenantId = tenantId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("GradesQuery")]
        public async Task<IActionResult> GradesQueryPost(bool isGradeQueryOpen)
        {
            var tenantId = User.FindFirstValue("TenantId");
            await EnsureSettingWithTenant("IsGradeQueryOpen", isGradeQueryOpen ? "true" : "false", tenantId!);

            TempData["SuccessMessage"] = "تم تحديث إعدادات استعلام الدرجات.";
            return RedirectToAction(nameof(GradesQuery));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(bool isRegistrationOpen, string registrationSuccessMessage, string whatsappGroupLink, string ageReferenceDate)
        {
            var tenantId = User.FindFirstValue("TenantId");

            await EnsureSettingWithTenant("IsRegistrationOpen", isRegistrationOpen ? "true" : "false", tenantId!);
            await EnsureSettingWithTenant("RegistrationSuccessMessage", registrationSuccessMessage ?? "", tenantId!);
            await EnsureSettingWithTenant("WhatsAppGroupLink", whatsappGroupLink ?? "", tenantId!);
            await EnsureSettingWithTenant("AgeReferenceDate", ageReferenceDate ?? "", tenantId!);

            TempData["SuccessMessage"] = "تم تحديث إعدادات الفورم بنجاح.";
            return RedirectToAction("Index");
        }

        private async Task EnsureSettingWithTenant(string key, string value, string tenantId)
        {
            var existing = await _context.SystemSettings
                .FindAsync(tenantId, key);

            if (existing != null)
            {
                existing.Value = value;
                _context.SystemSettings.Update(existing);
            }
            else
            {
                _context.SystemSettings.Add(new SystemSetting
                {
                    TenantId = tenantId,
                    Key = key,
                    Value = value
                });
            }
            await _context.SaveChangesAsync();
        }
    }
}
