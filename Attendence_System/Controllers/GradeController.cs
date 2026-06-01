using Attendence_System.Models;
using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class GradeController : Controller
    {
        private readonly IGradeService _gradeService;

        public GradeController(IGradeService gradeService)
        {
            _gradeService = gradeService;
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
            {
                return Json(new { success = true, message = "تم الحذف بنجاح." });
            }
            return Json(new { success = false, message = "حدث خطأ أثناء الحذف." });
        }
    }
}
