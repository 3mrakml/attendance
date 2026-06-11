using Attendence_System.Data;
using Attendence_System.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Attendence_System.Services
{
    public class ExamService : IExamService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExamService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetTenantId() =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue("TenantId") ?? string.Empty;

        public async Task<List<Exam>> GetAllExamsAsync()
        {
            return await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Grade)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<Exam?> GetExamWithStudentsAsync(int examId)
        {
            return await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Grade)
                .Include(e => e.StudentExams)
                    .ThenInclude(se => se.Student)
                .FirstOrDefaultAsync(e => e.ExamId == examId);
        }

        public async Task CreateExamAsync(Exam exam, string tenantId)
        {
            exam.TenantId = tenantId;
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();
        }

        public async Task<List<StudentExam>> GetOrCreateStudentExamsAsync(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Grade)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return new List<StudentExam>();

            // جلب كل طلاب الصف المرتبط بالامتحان
            var students = await _context.Students
                .Where(s => s.GradeId == exam.GradeId)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            // جلب الإجابات الموجودة
            var existing = await _context.StudentExams
                .Where(se => se.ExamId == examId)
                .ToListAsync();

            // إنشاء سجلات فارغة للطلاب الجدد
            var existingIds = existing.Select(se => se.StudentId).ToHashSet();
            var toAdd = students
                .Where(s => !existingIds.Contains(s.StudentId))
                .Select(s => new StudentExam { StudentId = s.StudentId, ExamId = examId, Score = null })
                .ToList();

            if (toAdd.Any())
            {
                _context.StudentExams.AddRange(toAdd);
                await _context.SaveChangesAsync();
            }

            // إعادة جلب كل السجلات مع بيانات الطالب
            return await _context.StudentExams
                .Include(se => se.Student)
                .Where(se => se.ExamId == examId)
                .OrderBy(se => se.Student!.FullName)
                .ToListAsync();
        }

        public async Task SaveStudentScoresAsync(int examId, Dictionary<int, double?> scores)
        {
            var studentExams = await _context.StudentExams
                .Where(se => se.ExamId == examId)
                .ToListAsync();

            foreach (var se in studentExams)
            {
                if (scores.TryGetValue(se.StudentExamId, out var score))
                    se.Score = score;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteExamAsync(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam != null)
            {
                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<StudentExam>> GetStudentExamsAsync(int studentId)
        {
            return await _context.StudentExams
                .Include(se => se.Exam)
                    .ThenInclude(e => e!.Course)
                .Include(se => se.Exam)
                    .ThenInclude(e => e!.Grade)
                .Where(se => se.StudentId == studentId)
                .OrderByDescending(se => se.Exam!.Date)
                .ToListAsync();
        }
    }
}
