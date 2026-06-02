using System.Linq;
using System.Threading.Tasks;
using Attendence_System.Models;
using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendence_System.Controllers
{
    [AllowAnonymous]
    public class PublicController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IGradeService _gradeService;
        private readonly ISystemSettingService _settingService;
        private readonly IQRCodeService _qrCodeService;

        public PublicController(
            IStudentService studentService, 
            IGradeService gradeService, 
            ISystemSettingService settingService,
            IQRCodeService qrCodeService)
        {
            _studentService = studentService;
            _gradeService = gradeService;
            _settingService = settingService;
            _qrCodeService = qrCodeService;
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var isRegistrationOpenStr = await _settingService.GetSettingAsync("IsRegistrationOpen", "false");
            if (isRegistrationOpenStr != "true")
            {
                return View("RegistrationClosed");
            }

            ViewBag.Grades = await _gradeService.GetAllGradesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Student model)
        {
            var isRegistrationOpenStr = await _settingService.GetSettingAsync("IsRegistrationOpen", "false");
            if (isRegistrationOpenStr != "true")
            {
                return View("RegistrationClosed");
            }

            ModelState.Remove("QRToken");

            if (!ModelState.IsValid)
            {
                ViewBag.Grades = await _gradeService.GetAllGradesAsync();
                return View(model);
            }

            // Check if phone number already exists
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                var existingStudent = await _studentService.GetStudentByPhoneNumberAsync(model.PhoneNumber);
                if (existingStudent != null)
                {
                    // User already registered, redirect to success to show their card
                    return RedirectToAction("RegistrationSuccess", new { id = existingStudent.StudentId });
                }
            }

            // Generate unique 4-digit QRToken
            string token;
            do { token = System.Random.Shared.Next(1000, 10000).ToString(); }
            while (await _studentService.StudentExistsAsync(token));
            model.QRToken = token;

            var newStudent = await _studentService.CreateStudentAsync(model);

            return RedirectToAction("RegistrationSuccess", new { id = newStudent.StudentId });
        }

        [HttpGet]
        public async Task<IActionResult> RegistrationSuccess(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // Load grade details
            if (student.GradeId > 0)
            {
                var grades = await _gradeService.GetAllGradesAsync();
                student.Grade = grades.FirstOrDefault(g => g.GradeId == student.GradeId);
            }

            var successMessage = await _settingService.GetSettingAsync("RegistrationSuccessMessage", "تم التسجيل بنجاح! احتفظ بالباركود الخاص بك.");
            var whatsappGroupLink = await _settingService.GetSettingAsync("WhatsAppGroupLink", "");
            
            ViewBag.SuccessMessage = successMessage;
            ViewBag.WhatsAppGroupLink = whatsappGroupLink;
            ViewBag.QRCode = _qrCodeService.GenerateQRCode(student.QRToken);

            return View(student);
        }
    }
}
