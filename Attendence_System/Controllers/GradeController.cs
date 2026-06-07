using Attendence_System.Models;
using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class GradeController : Controller
    {
        private readonly IGradeService _gradeService;
        private readonly UserManager<AppUser> _userManager;

        public GradeController(IGradeService gradeService, UserManager<AppUser> userManager)
        {
            _gradeService = gradeService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var grades = await _gradeService.GetAllGradesAsync();
            return View(grades);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Grade grade)
        {
            var tenantId = User.FindFirstValue("TenantId");
            grade.TenantId = tenantId!;

            ModelState.Remove("TenantId");
            ModelState.Remove("Tenant");

            if (ModelState.IsValid)
            {
                await _gradeService.AddGradeAsync(grade);
                TempData["SuccessMessage"] = "تم إضافة الصف بنجاح.";
            }
            else
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء إضافة الصف.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _gradeService.DeleteGradeAsync(id);
            if (result)
                return Json(new { success = true, message = "تم الحذف بنجاح." });

            return Json(new { success = false, message = "حدث خطأ أثناء الحذف." });
        }
    }
}
