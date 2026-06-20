using Attendence_System.Models;
using Attendence_System.Services;
using Attendence_System.Filters;
using Attendence_System.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Attendence_System.Controllers
{
    [Authorize]
    [AutoClearStudentCache]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IQRCodeService _qrCodeService;
        private readonly IGradeService _gradeService;
        private readonly IImportService _importService;
        private readonly ISystemSettingService _settingService;
        private readonly IMemoryCache _cache;

        public StudentController(
            IStudentService studentService,
            IQRCodeService qrCodeService,
            IGradeService gradeService,
            IImportService importService,
            ISystemSettingService settingService,
            IMemoryCache cache)
        {
            _studentService = studentService;
            _qrCodeService = qrCodeService;
            _gradeService = gradeService;
            _importService = importService;
            _settingService = settingService;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, int? gradeId, int page = 1, int? sortCol = null, bool sortAsc = true)
        {
            var tenantId = User.FindFirstValue("TenantId");
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            string studentsCacheKey = $"students_index_{tenantId}";
            string attCacheKey = $"attendance_perc_{tenantId}";

            if (!_cache.TryGetValue(studentsCacheKey, out List<Student>? allStudents) || allStudents == null)
            {
                allStudents = await _studentService.GetAllStudentsAsync();
                _cache.Set(studentsCacheKey, allStudents, TimeSpan.FromMinutes(30));
            }

            if (!_cache.TryGetValue(attCacheKey, out Dictionary<int, double>? attendancePercentages) || attendancePercentages == null)
            {
                attendancePercentages = await _studentService.GetStudentsAttendancePercentagesAsync();
                _cache.Set(attCacheKey, attendancePercentages, TimeSpan.FromMinutes(30));
            }

            var filteredStudents = allStudents.AsEnumerable();
            if (!string.IsNullOrEmpty(searchString))
            {
                filteredStudents = filteredStudents.Where(s =>
                    s.FullName.ContainsArabicFuzzy(searchString) ||
                    (s.QRToken != null && s.QRToken.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (s.PhoneNumber != null && s.PhoneNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                );
            }
            if (gradeId.HasValue)
            {
                filteredStudents = filteredStudents.Where(s => s.GradeId == gradeId.Value);
            }

            if (sortCol.HasValue)
            {
                Func<Student, object> keySelector = sortCol.Value switch
                {
                    0 => s => s.FullName,
                    1 => s => s.Grade?.Name ?? "",
                    2 => s => attendancePercentages.ContainsKey(s.StudentId) ? attendancePercentages[s.StudentId] : 0,
                    3 => s => s.QRToken ?? "",
                    _ => s => s.FullName
                };

                filteredStudents = sortAsc 
                    ? filteredStudents.OrderBy(keySelector) 
                    : filteredStudents.OrderByDescending(keySelector);
            }

            int pageSize = 50;
            var totalItems = filteredStudents.Count();
            var pagedStudents = filteredStudents.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var codeType = await _settingService.GetSettingAsync("CodeType", "QR");
            ViewBag.CodeType = codeType;
            ViewBag.Grades = await _gradeService.GetAllGradesAsync();
            ViewBag.AttendancePercentages = attendancePercentages;
            ViewBag.AgeReferenceDate = await _settingService.GetSettingAsync("AgeReferenceDate", "");

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.SearchString = searchString;
            ViewBag.GradeId = gradeId;
            ViewBag.TotalItems = totalItems;
            ViewBag.SortCol = sortCol;
            ViewBag.SortAsc = sortAsc;

            return View(pagedStudents);
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
        public async Task<IActionResult> PrintAllCards(string searchString, int? gradeId)
        {
            var students = await _studentService.GetAllStudentsAsync();

            var filteredStudents = students.AsEnumerable();
            if (!string.IsNullOrEmpty(searchString))
            {
                filteredStudents = filteredStudents.Where(s =>
                    s.FullName.ContainsArabicFuzzy(searchString) ||
                    (s.QRToken != null && s.QRToken.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (s.PhoneNumber != null && s.PhoneNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                );
            }
            if (gradeId.HasValue)
            {
                filteredStudents = filteredStudents.Where(s => s.GradeId == gradeId.Value);
            }

            var codeType = await _settingService.GetSettingAsync("CodeType", "QR");
            ViewBag.CodeType = codeType;

            return View(filteredStudents.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> PrintSelectedQRCodes(List<int> studentIds)
        {
            if (studentIds == null || !studentIds.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var allStudents = await _studentService.GetAllStudentsAsync();
            var selectedStudents = allStudents.Where(s => studentIds.Contains(s.StudentId)).ToList();

            var codeType = await _settingService.GetSettingAsync("CodeType", "QR");
            ViewBag.CodeType = codeType;

            return View("ExportQRCodes", selectedStudents);
        }

        // ─── Export QR Codes page ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ExportQRCodes(string searchString, int? gradeId)
        {
            var students = await _studentService.GetAllStudentsAsync();

            var filteredStudents = students.AsEnumerable();
            if (!string.IsNullOrEmpty(searchString))
            {
                filteredStudents = filteredStudents.Where(s =>
                    s.FullName.ContainsArabicFuzzy(searchString) ||
                    (s.QRToken != null && s.QRToken.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (s.PhoneNumber != null && s.PhoneNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                );
            }
            if (gradeId.HasValue)
            {
                filteredStudents = filteredStudents.Where(s => s.GradeId == gradeId.Value);
            }

            var codeType = await _settingService.GetSettingAsync("CodeType", "QR");
            ViewBag.CodeType = codeType;

            return View(filteredStudents.ToList());
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

        [HttpGet]
        public async Task<IActionResult> DownloadCode(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var codeType = await _settingService.GetSettingAsync("CodeType", "QR");
            string base64String = codeType == "Barcode" 
                ? _qrCodeService.GenerateBarcode(student.QRToken) 
                : _qrCodeService.GenerateQRCode(student.QRToken);

            if (string.IsNullOrEmpty(base64String))
            {
                return NotFound();
            }

            var parts = base64String.Split(',');
            if (parts.Length != 2)
            {
                return NotFound();
            }

            byte[] imageBytes = Convert.FromBase64String(parts[1]);
            return File(imageBytes, "image/png", $"Student_Code_{student.QRToken}.png");
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
