using Attendence_System.Data;
using Attendence_System.Models;
using Attendence_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendence_System.Controllers
{
    [AllowAnonymous]
    [Route("Public/{username}/[action]")]
    public class PublicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IQRCodeService _qrCodeService;
        private readonly UserManager<AppUser> _userManager;

        public PublicController(
            ApplicationDbContext context,
            IQRCodeService qrCodeService,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Register(string username)
        {
            if (string.IsNullOrEmpty(username)) return NotFound("Username is required.");
            var user = await _userManager.FindByNameAsync(username);
            if (user == null || user.TenantId == null) return NotFound("Teacher not found.");

            // Load settings for this specific tenant (bypass global filter)
            var isRegistrationOpenStr = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == user.TenantId && s.Key == "IsRegistrationOpen")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "false";

            if (isRegistrationOpenStr != "true")
                return View("RegistrationClosed");

            // Load grades for this specific tenant (bypass global filter)
            var grades = await _context.Grades
                .IgnoreQueryFilters()
                .Where(g => g.TenantId == user.TenantId)
                .ToListAsync();

            ViewBag.TeacherUsername = username;
            ViewBag.Grades = grades;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, Student model)
        {
            if (string.IsNullOrEmpty(username)) return NotFound("Username is required.");
            var user = await _userManager.FindByNameAsync(username);
            if (user == null || user.TenantId == null) return NotFound("Teacher not found.");

            var isRegistrationOpenStr = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == user.TenantId && s.Key == "IsRegistrationOpen")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "false";

            if (isRegistrationOpenStr != "true")
                return View("RegistrationClosed");

            ModelState.Remove("QRToken");
            ModelState.Remove("TenantId");
            ModelState.Remove("Tenant");

            if (!ModelState.IsValid)
            {
                var grades = await _context.Grades
                    .IgnoreQueryFilters()
                    .Where(g => g.TenantId == user.TenantId)
                    .ToListAsync();

                ViewBag.TeacherUsername = username;
                ViewBag.Grades = grades;
                return View(model);
            }

            model.TenantId = user.TenantId;

            // Check if phone number already exists for this tenant
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                var existingStudent = await _context.Students
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.PhoneNumber == model.PhoneNumber && s.TenantId == user.TenantId);

                if (existingStudent != null)
                    return RedirectToAction("RegistrationSuccess", new { username, id = existingStudent.StudentId });
            }

            // Generate Sequential QRToken for this tenant based on GradeId
            string prefix = model.GradeId.ToString();
            int expectedLength = prefix.Length + 3;
            
            var existingTokens = await _context.Students
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == user.TenantId && s.GradeId == model.GradeId && s.QRToken.StartsWith(prefix) && s.QRToken.Length == expectedLength)
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
                    .AnyAsync(s => s.QRToken == token && s.TenantId == user.TenantId);
                if (!exists) break;
            }

            model.QRToken = token;

            _context.Students.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("RegistrationSuccess", new { username, id = model.StudentId });
        }

        [HttpGet]
        public async Task<IActionResult> RegistrationSuccess(string username, int id)
        {
            if (string.IsNullOrEmpty(username)) return NotFound();
            var user = await _userManager.FindByNameAsync(username);
            if (user == null || user.TenantId == null) return NotFound();

            var student = await _context.Students
                .IgnoreQueryFilters()
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.StudentId == id && s.TenantId == user.TenantId);

            if (student == null)
                return NotFound();

            var successMessage = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == user.TenantId && s.Key == "RegistrationSuccessMessage")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "تم التسجيل بنجاح! احتفظ بالباركود الخاص بك.";

            var whatsappGroupLink = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == user.TenantId && s.Key == "WhatsAppGroupLink")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "";

            ViewBag.SuccessMessage = successMessage;
            ViewBag.WhatsAppGroupLink = whatsappGroupLink;
            ViewBag.QRCode = _qrCodeService.GenerateQRCode(student.QRToken);

            return View(student);
        }

        // ─── Grade Query (Public) ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Grades(string username)
        {
            if (string.IsNullOrEmpty(username)) return NotFound();
            var user = await _userManager.FindByNameAsync(username);
            if (user == null || user.TenantId == null) return NotFound();

            var isOpenStr = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == user.TenantId && s.Key == "IsGradeQueryOpen")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "false";

            if (isOpenStr != "true")
                return View("GradeQueryClosed");

            ViewBag.TeacherUsername = username;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grades(string username, string query)
        {
            if (string.IsNullOrEmpty(username)) return NotFound();
            var user = await _userManager.FindByNameAsync(username);
            if (user == null || user.TenantId == null) return NotFound();

            var isOpenStr = await _context.SystemSettings
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == user.TenantId && s.Key == "IsGradeQueryOpen")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "false";

            if (isOpenStr != "true")
                return View("GradeQueryClosed");

            query = query?.Trim() ?? "";

            // Find student by QR token or phone number (for this tenant only)
            var student = await _context.Students
                .IgnoreQueryFilters()
                .Include(s => s.Grade)
                .Where(s => s.TenantId == user.TenantId &&
                            (s.QRToken == query || s.PhoneNumber == query))
                .FirstOrDefaultAsync();

            ViewBag.TeacherUsername = username;
            ViewBag.Query = query;

            if (student == null)
            {
                ViewBag.NotFound = true;
                return View();
            }

            // Load exams and scores for this student
            var studentExams = await _context.StudentExams
                .IgnoreQueryFilters()
                .Include(se => se.Exam)
                    .ThenInclude(e => e!.Course)
                .Include(se => se.Exam)
                    .ThenInclude(e => e!.Grade)
                .Where(se => se.StudentId == student.StudentId)
                .OrderByDescending(se => se.Exam!.Date)
                .ToListAsync();

            ViewBag.StudentExams = studentExams;
            return View("GradesResult", student);
        }
    }
}
