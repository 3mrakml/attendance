using Attendence_System.Models;
using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Attendence_System.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IQRCodeService _qrCodeService;
        private readonly IGradeService _gradeService;
        private readonly IImportService _importService;
        private readonly ISystemSettingService _settingService;

        public StudentController(
            IStudentService studentService,
            IQRCodeService qrCodeService,
            IGradeService gradeService,
            IImportService importService,
            ISystemSettingService settingService)
        {
            _studentService = studentService;
            _qrCodeService = qrCodeService;
            _gradeService = gradeService;
            _importService = importService;
            _settingService = settingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var students = await _studentService.GetAllStudentsAsync();

            var codeType = await _settingService.GetSettingAsync("CodeType", "QR");
            ViewBag.CodeType = codeType;

            ViewBag.StudentQRCodes = students.ToDictionary(
                s => s.StudentId,
                s => codeType == "Barcode" ? _qrCodeService.GenerateBarcode(s.QRToken) : _qrCodeService.GenerateQRCode(s.QRToken)
            );

            ViewBag.Grades = await _gradeService.GetAllGradesAsync();
            ViewBag.AttendancePercentages = await _studentService.GetStudentsAttendancePercentagesAsync();
            ViewBag.AgeReferenceDate = await _settingService.GetSettingAsync("AgeReferenceDate", "");

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

                model.QRToken = await _studentService.GenerateSequentialQRTokenAsync(model.GradeId);

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

        // ─── Print All Cards ───────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> PrintAllCards()
        {
            var students = await _studentService.GetAllStudentsAsync();

            var codeType = await _settingService.GetSettingAsync("CodeType", "QR");
            ViewBag.CodeType = codeType;

            ViewBag.StudentQRCodes = students.ToDictionary(
                s => s.StudentId,
                s => codeType == "Barcode" ? _qrCodeService.GenerateBarcode(s.QRToken) : _qrCodeService.GenerateQRCode(s.QRToken)
            );

            return View(students);
        }

        // ─── Export QR Codes page ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ExportQRCodes()
        {
            var students = await _studentService.GetAllStudentsAsync();

            var codeType = await _settingService.GetSettingAsync("CodeType", "QR");
            ViewBag.CodeType = codeType;

            ViewBag.StudentQRCodes = students.ToDictionary(
                s => s.StudentId,
                s => codeType == "Barcode" ? _qrCodeService.GenerateBarcode(s.QRToken) : _qrCodeService.GenerateQRCode(s.QRToken)
            );

            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> PrintSingleQR(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var codeType = await _settingService.GetSettingAsync("CodeType", "QR");
            ViewBag.CodeType = codeType;
            ViewBag.QRCode = codeType == "Barcode" ? _qrCodeService.GenerateBarcode(student.QRToken) : _qrCodeService.GenerateQRCode(student.QRToken);
            return View(student);
        }

        // ─── Import from Excel ─────────────────────────────────────────────────


        [HttpGet]
        public async Task<IActionResult> ImportTemplate()
        {
            var grades = await _gradeService.GetAllGradesAsync();
            var gradeNames = grades.Select(g => g.Name);
            var fileBytes = _importService.GenerateStudentTemplate(gradeNames);
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "نموذج_استيراد_الطلاب.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ImportError"] = "يرجى اختيار ملف Excel أولاً.";
                return RedirectToAction(nameof(Index));
            }

            var ext = Path.GetExtension(excelFile.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
            {
                TempData["ImportError"] = "صيغة الملف غير مدعومة. يرجى رفع ملف Excel (.xlsx)";
                return RedirectToAction(nameof(Index));
            }

            var tenantId = User.FindFirstValue("TenantId");
            if (string.IsNullOrEmpty(tenantId))
            {
                TempData["ImportError"] = "حدث خطأ في جلسة المستخدم. يرجى تسجيل الدخول مجدداً.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _importService.ImportStudentsFromExcelAsync(excelFile, tenantId);
                TempData["ImportAdded"] = result.AddedCount;
                if (result.HasErrors)
                    TempData["ImportErrors"] = JsonSerializer.Serialize(result.Errors);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message
                          ?? ex.InnerException?.Message
                          ?? ex.Message;
                TempData["ImportError"] = $"خطأ: {ex.GetType().Name} — {inner}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
