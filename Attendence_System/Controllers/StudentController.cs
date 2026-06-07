using Attendence_System.Models;
using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IQRCodeService _qrCodeService;
        private readonly IGradeService _gradeService;

        public StudentController(
            IStudentService studentService,
            IQRCodeService qrCodeService,
            IGradeService gradeService)
        {
            _studentService = studentService;
            _qrCodeService = qrCodeService;
            _gradeService = gradeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var students = await _studentService.GetAllStudentsAsync();

            ViewBag.StudentQRCodes = students.ToDictionary(
                s => s.StudentId,
                s => _qrCodeService.GenerateQRCode(s.QRToken)
            );

            ViewBag.Grades = await _gradeService.GetAllGradesAsync();
            ViewBag.AttendancePercentages = await _studentService.GetStudentsAttendancePercentagesAsync();

            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Grades = await _gradeService.GetAllGradesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student model)
        {
            ModelState.Remove("QRToken");
            ModelState.Remove("TenantId");
            ModelState.Remove("Tenant");

            if (ModelState.IsValid)
            {
                var tenantId = User.FindFirstValue("TenantId");
                model.TenantId = tenantId!;

                string token;
                do { token = System.Random.Shared.Next(1000, 10000).ToString(); }
                while (await _studentService.StudentExistsAsync(token));
                model.QRToken = token;

                await _studentService.CreateStudentAsync(model);
                TempData["SuccessMessage"] = "تم إضافة الطالب بنجاح.";
                return RedirectToAction("Index");
            }
            TempData["ErrorMessage"] = "حدث خطأ أثناء إضافة الطالب. يرجى التحقق من البيانات.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Student model)
        {
            ModelState.Remove("QRToken");
            ModelState.Remove("TenantId");
            ModelState.Remove("Tenant");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء تعديل الطالب. يرجى التحقق من البيانات.";
                return RedirectToAction("Index");
            }

            var success = await _studentService.UpdateStudentAsync(model);
            if (!success)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على الطالب المطلوب.";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "تم تعديل بيانات الطالب بنجاح.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Report(int id)
        {
            var report = await _studentService.GetStudentReportAsync(id);
            if (report == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على الطالب المكتوب أو لا تملك صلاحية الوصول إليه.";
                return RedirectToAction("Index");
            }
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> PrintCard(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
                return NotFound();

            if (student.GradeId > 0)
            {
                var grades = await _gradeService.GetAllGradesAsync();
                var grade = grades.FirstOrDefault(g => g.GradeId == student.GradeId);
                if (grade != null)
                    student.Grade = grade;
            }

            ViewBag.QRCode = _qrCodeService.GenerateQRCode(student.QRToken);
            return View(student);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _studentService.DeleteStudentAsync(id);
            if (!success)
                return NotFound();

            return Ok();
        }
    }
}
