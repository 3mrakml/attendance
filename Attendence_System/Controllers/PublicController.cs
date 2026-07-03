using Attendence_System.Data;
using Attendence_System.Models;
using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Attendence_System.Controllers
{
    [AllowAnonymous]
    [Route("Public/{tenantId}/[action]")]
    public class PublicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IQRCodeService _qrCodeService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMemoryCache _cache;

        public PublicController(
            ApplicationDbContext context,
            IQRCodeService qrCodeService,
            UserManager<AppUser> userManager,
            IMemoryCache cache)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _userManager = userManager;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Register(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId)) return NotFound("Tenant ID is required.");

            // ── Run queries sequentially to prevent EF Core concurrency exception on single DbContext ──
            var settingKeys = new[]
            {
                "IsRegistrationOpen", "AgeReferenceDate", "ShowPhoneNumberField",
                "ShowDateOfBirthField", "ShowAgeField", "ShowGradeField"
            };

            var tenantExists = await _context.Tenants.AnyAsync(t => t.Id == tenantId);
            var grades       = await _context.Grades.IgnoreQueryFilters().Where(g => g.TenantId == tenantId).ToListAsync();
            var settings     = await _context.SystemSettings.IgnoreQueryFilters()
                                 .Where(s => s.TenantId == tenantId && settingKeys.Contains(s.Key))
                                 .ToDictionaryAsync(s => s.Key, s => s.Value);

            if (!tenantExists) return NotFound("Teacher not found.");

            string Get(string key, string def) => settings.TryGetValue(key, out var v) ? v : def;

            if (Get("IsRegistrationOpen", "false") != "true")
                return View("RegistrationClosed");

            ViewBag.TenantId              = tenantId;
            ViewBag.Grades                = grades;
            ViewBag.AgeReferenceDate      = Get("AgeReferenceDate", null!);
            ViewBag.ShowPhoneNumberField   = Get("ShowPhoneNumberField", "true") == "true";
            ViewBag.ShowDateOfBirthField   = Get("ShowDateOfBirthField", "true") == "true";
            ViewBag.ShowAgeField           = Get("ShowAgeField", "true") == "true";
            ViewBag.ShowGradeField         = Get("ShowGradeField", "true") == "true";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string tenantId, Student model)
        {
            if (string.IsNullOrEmpty(tenantId)) return NotFound("Tenant ID is required.");
            var tenantExists = await _context.Tenants.AnyAsync(t => t.Id == tenantId);
            if (!tenantExists) return NotFound("Teacher not found.");

            var isRegistrationOpenStr = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.Key == "IsRegistrationOpen")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "false";

            if (isRegistrationOpenStr != "true")
                return View("RegistrationClosed");

            ModelState.Remove("QRToken");
            ModelState.Remove("TenantId");
            ModelState.Remove("Tenant");

            var showGradeFieldStr = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.Key == "ShowGradeField")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "true";

            if (showGradeFieldStr != "true")
            {
                var defaultGradeIdStr = await _context.SystemSettings
                    .IgnoreQueryFilters()
                    .Where(s => s.TenantId == tenantId && s.Key == "DefaultGradeId")
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync();

                if (int.TryParse(defaultGradeIdStr, out int defaultGradeId))
                {
                    model.GradeId = defaultGradeId;
                    ModelState.Remove("GradeId");
                }
            }

            if (!ModelState.IsValid)
            {
                // ── Reload form data with batched parallel queries ──
                var reloadSettingKeys = new[]
                {
                    "AgeReferenceDate", "ShowPhoneNumberField",
                    "ShowDateOfBirthField", "ShowAgeField"
                };
                var grades = await _context.Grades.IgnoreQueryFilters().Where(g => g.TenantId == tenantId).ToListAsync();
                var rs     = await _context.SystemSettings.IgnoreQueryFilters()
                                 .Where(s => s.TenantId == tenantId && reloadSettingKeys.Contains(s.Key))
                                 .ToDictionaryAsync(s => s.Key, s => s.Value);
                string RGet(string key, string def) => rs.TryGetValue(key, out var v) ? v : def;

                ViewBag.TenantId             = tenantId;
                ViewBag.Grades               = grades;
                ViewBag.AgeReferenceDate      = RGet("AgeReferenceDate", null!);
                ViewBag.ShowPhoneNumberField   = RGet("ShowPhoneNumberField", "true") == "true";
                ViewBag.ShowDateOfBirthField   = RGet("ShowDateOfBirthField", "true") == "true";
                ViewBag.ShowAgeField           = RGet("ShowAgeField", "true") == "true";
                ViewBag.ShowGradeField         = showGradeFieldStr == "true";
                return View(model);
            }

            model.TenantId = tenantId;

            // Check if phone number already exists for this tenant
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                var existingStudent = await _context.Students
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.PhoneNumber == model.PhoneNumber && s.TenantId == tenantId);

                if (existingStudent != null)
                {
                    var encodedId = HttpContext.RequestServices.GetRequiredService<IHashidService>().Encode(existingStudent.StudentId);
                    return RedirectToAction("RegistrationSuccess", new { tenantId, id = encodedId });
                }
            }

            // Generate Sequential QRToken for this tenant based on GradeId
            string prefix = model.GradeId.ToString();
            int expectedLength = prefix.Length + 3;
            
            var existingTokens = await _context.Students
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.GradeId == model.GradeId && s.QRToken.StartsWith(prefix) && s.QRToken.Length == expectedLength)
                .Select(s => s.QRToken)
                .ToListAsync();
                
            int maxSeq = 0;
            foreach (var t in existingTokens)
            {
                if (int.TryParse(t.Substring(prefix.Length), out int seq))
                {
                    if (seq > maxSeq) maxSeq = seq;
                }
            }
            
            int nextSeq = maxSeq;
            string token;
            while (true)
            {
                nextSeq++;
                token = $"{model.GradeId}{nextSeq:D3}";
                bool exists = await _context.Students
                    .IgnoreQueryFilters()
                    .AnyAsync(s => s.QRToken == token && s.TenantId == tenantId);
                if (!exists) break;
            }

            model.QRToken = token;

            _context.Students.Add(model);
            await _context.SaveChangesAsync();

            // Clear cache so the student appears immediately in Student Management
            _cache.Remove($"students_index_{tenantId}");
            _cache.Remove($"attendance_perc_{tenantId}");
            _cache.Remove($"comprehensive_report_{tenantId}");

            var finalEncodedId = HttpContext.RequestServices.GetRequiredService<IHashidService>().Encode(model.StudentId);
            return RedirectToAction("RegistrationSuccess", new { tenantId, id = finalEncodedId });
        }

        [HttpGet]
        public async Task<IActionResult> RegistrationSuccess(string tenantId, int id)
        {
            if (string.IsNullOrEmpty(tenantId)) return NotFound();
            var tenantExists = await _context.Tenants.AnyAsync(t => t.Id == tenantId);
            if (!tenantExists) return NotFound();

            var student = await _context.Students
                .IgnoreQueryFilters()
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.StudentId == id && s.TenantId == tenantId);

            if (student == null)
                return NotFound();

            var successMessage = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.Key == "RegistrationSuccessMessage")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "تم التسجيل بنجاح! احتفظ بالباركود الخاص بك.";

            var whatsappGroupLink = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.Key == "WhatsAppGroupLink")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "";

            ViewBag.SuccessMessage = successMessage;
            ViewBag.WhatsAppGroupLink = whatsappGroupLink;
            ViewBag.QRCode = _qrCodeService.GenerateQRCode(student.QRToken);

            return View(student);
        }

        // ─── Grade Query (Public) ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Grades(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId)) return NotFound();
            var tenantExists = await _context.Tenants.AnyAsync(t => t.Id == tenantId);
            if (!tenantExists) return NotFound();

            var isOpenStr = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.Key == "IsGradeQueryOpen")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "false";

            if (isOpenStr != "true")
                return View("GradeQueryClosed");

            ViewBag.TenantId = tenantId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grades(string tenantId, string query)
        {
            if (string.IsNullOrEmpty(tenantId)) return NotFound();
            var tenantExists = await _context.Tenants.AnyAsync(t => t.Id == tenantId);
            if (!tenantExists) return NotFound();

            var isOpenStr = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.Key == "IsGradeQueryOpen")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "false";

            if (isOpenStr != "true")
                return View("GradeQueryClosed");

            query = query?.Trim() ?? "";

            // Find student by QR token or phone number (for this tenant only)
            var student = await _context.Students
                .IgnoreQueryFilters()
                .Include(s => s.Grade)
                .Where(s => s.TenantId == tenantId &&
                            (s.QRToken == query || s.PhoneNumber == query))
                .FirstOrDefaultAsync();

            ViewBag.TenantId = tenantId;
            ViewBag.Query = query;

            if (student == null)
            {
                ViewBag.NotFound = true;
                return View();
            }

            // ── Run queries sequentially to prevent EF Core concurrency exception ──
            var studentExams = await _context.StudentExams
                .IgnoreQueryFilters()
                .Include(se => se.Exam).ThenInclude(e => e!.Course)
                .Include(se => se.Exam).ThenInclude(e => e!.Grade)
                .Where(se => se.StudentId == student.StudentId)
                .OrderByDescending(se => se.Exam!.Date)
                .ToListAsync();

            var topLectures = await _context.Lectures
                .IgnoreQueryFilters()
                .Where(l => l.Course.TenantId == tenantId &&
                            l.LectureGrades.Any(lg => lg.GradeId == student.GradeId))
                .OrderByDescending(l => l.DateTime)
                .Select(l => new { l.LectureId, CourseName = l.Course != null ? l.Course.Name : null, l.DateTime })
                .Take(10)
                .ToListAsync();

            var totalLecturesCount = await _context.LectureGrades
                .IgnoreQueryFilters()
                .Where(lg => lg.GradeId == student.GradeId &&
                             lg.Lecture.Course.TenantId == tenantId)
                .CountAsync();

            var attendedLectureIds = await _context.StudentLectures
                .IgnoreQueryFilters()
                .Where(sl => sl.StudentId == student.StudentId)
                .Select(sl => sl.LectureId)
                .ToListAsync();

            var settingKey = $"Grade_{student.GradeId}_Marks";
            var marksStr = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.Key == settingKey)
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            var attendedLecturesCount = attendedLectureIds.Count;

            // Convert projection to Lecture-like objects for the view
            var allGradeLectures = topLectures.Select(l => new Attendence_System.Models.Lecture
            {
                LectureId = l.LectureId,
                DateTime  = l.DateTime,
                Course    = l.CourseName != null ? new Attendence_System.Models.Course { Name = l.CourseName } : null
            }).ToList();

            ViewBag.StudentExams       = studentExams;
            ViewBag.AllLectures        = allGradeLectures;
            ViewBag.AttendedLectureIds = attendedLectureIds;
            ViewBag.TotalLectures      = totalLecturesCount;
            ViewBag.AttendedLectures   = attendedLecturesCount;

            double attendanceMaxMarks  = double.TryParse(marksStr, out var parsed) ? parsed : 10;
            double attendancePercentage = totalLecturesCount > 0
                ? ((double)attendedLecturesCount / totalLecturesCount) * 100 : 0;
            double attendanceScore = Math.Round((attendancePercentage / 100.0) * attendanceMaxMarks, 2);

            ViewBag.AttendanceMaxMarks = attendanceMaxMarks;
            ViewBag.AttendanceScore    = attendanceScore;

            return View("GradesResult", student);
        }
    }
}
