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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Grade grade)
        {
            ModelState.Remove("TenantId");
            ModelState.Remove("Tenant");

            if (ModelState.IsValid)
            {
                // Retrieve existing grade to preserve TenantId or other properties if needed
                var existingGrade = await _gradeService.GetGradeByIdAsync(grade.GradeId);
                if (existingGrade == null)
                {
                    TempData["ErrorMessage"] = "لم يتم العثور على الصف المطلوب.";
                    return RedirectToAction(nameof(Index));
                }

                // Update properties
                existingGrade.Name = grade.Name;
                existingGrade.Code = grade.Code;
                existingGrade.MinAge = grade.MinAge;
                existingGrade.MaxAge = grade.MaxAge;

                var result = await _gradeService.UpdateGradeAsync(existingGrade);
                if (result)
                {
                    TempData["SuccessMessage"] = "تم تعديل الصف بنجاح.";
                }
                else
                {
                    TempData["ErrorMessage"] = "حدث خطأ أثناء تعديل الصف.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "البيانات المدخلة غير صحيحة.";
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
