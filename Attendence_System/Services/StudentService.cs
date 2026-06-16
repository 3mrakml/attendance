using Attendence_System.Data;
using Attendence_System.Models;
using Attendence_System.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Attendence_System.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;

        public StudentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Student>> GetAllStudentsAsync()
        {
            // Global Query Filter automatically filters by TenantId
            return await _context.Students
                .Include(s => s.Grade)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetStudentCountByGradeAsync()
        {
            return await _context.Students
                .GroupBy(s => s.GradeId)
                .Select(g => new { GradeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.GradeId, g => g.Count);
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.StudentId == id);
        }

        public async Task<Student?> GetStudentByIdCollegeAsync(string qrToken)
        {
            return await _context.Students.FirstOrDefaultAsync(s => s.QRToken == qrToken);
        }

        public async Task<Student?> GetStudentByPhoneNumberAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
            return await _context.Students.FirstOrDefaultAsync(s => s.PhoneNumber == phoneNumber);
        }

        public async Task<bool> StudentExistsAsync(string qrToken)
        {
            return await _context.Students.AnyAsync(s => s.QRToken == qrToken);
        }

        public async Task<string> GenerateSequentialQRTokenAsync(int gradeId)
        {
            var grade = await _context.Grades.FindAsync(gradeId);
            string prefix = grade?.Code > 0 ? grade.Code.ToString() : gradeId.ToString();
            int expectedLength = prefix.Length + 3;
            
            var existingTokens = await _context.Students
                .Where(s => s.GradeId == gradeId && s.QRToken.StartsWith(prefix) && s.QRToken.Length == expectedLength)
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
            string newToken;
            while (true)
            {
                nextSeq++;
                newToken = $"{gradeId}{nextSeq:D3}";
                if (!await _context.Students.AnyAsync(s => s.QRToken == newToken))
                {
                    break;
                }
            }
            return newToken;
        }

        public async Task<Student> CreateStudentAsync(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<bool> UpdateStudentAsync(Student student)
        {
            var existingStudent = await _context.Students.FindAsync(student.StudentId);
            if (existingStudent == null) return false;

            existingStudent.FullName = student.FullName;
            existingStudent.Age = student.Age;
            existingStudent.GradeId = student.GradeId;
            existingStudent.PhoneNumber = student.PhoneNumber;
            existingStudent.DateOfBirth = student.DateOfBirth;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null) return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> RegisterAttendanceAsync(ScanRequestVM request)
        {
            var studentInfo = await _context.Students
                .Where(s => s.QRToken == request.IdCollege)
                .Select(s => new {
                    s.StudentId,
                    s.FullName,
                    s.GradeId,
                    IsAlreadyRegistered = _context.StudentLectures.Any(sl => sl.StudentId == s.StudentId && sl.LectureId == request.LectureId)
                })
                .FirstOrDefaultAsync();

            if (studentInfo == null)
                return (false, $"الطالب ذو الرمز {request.IdCollege} غير موجود.");

            if (studentInfo.IsAlreadyRegistered)
                return (false, $"{studentInfo.FullName} سجّل حضوره بالفعل.");

            var lectureInfo = await _context.Lectures
                .Where(l => l.LectureId == request.LectureId)
                .Select(l => new { 
                    l.IsAttendanceClosed, 
                    GradeIds = l.LectureGrades.Select(lg => lg.GradeId).ToList() 
                })
                .FirstOrDefaultAsync();

            if (lectureInfo == null)
                return (false, "المحاضرة غير موجودة.");

            if (lectureInfo.IsAttendanceClosed)
                return (false, "الغياب مغلق لهذه المحاضرة.");

            if (!lectureInfo.GradeIds.Contains(studentInfo.GradeId))
                return (false, $"الطالب ({studentInfo.FullName}) غير مسجل في صف هذه المحاضرة.");

            _context.StudentLectures.Add(new StudentLecture
            {
                StudentId = studentInfo.StudentId,
                LectureId = request.LectureId,
                AttendedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return (true, $"{studentInfo.FullName} تم تسجيل حضوره بنجاح!");
        }

        public async Task<List<StudentWithCount>> GetCourseAttendanceStatsAsync(int courseId)
        {
            var lectures = await _context.Lectures
                .Include(l => l.StudentLectures)
                    .ThenInclude(ls => ls.Student)
                .Where(d => d.CourseId == courseId)
                .ToListAsync();

            var studentsWithCount = lectures
                .SelectMany(l => l.StudentLectures)
                .GroupBy(ls => ls.Student.QRToken)
                .Select(g => new StudentWithCount
                {
                    StudentId = g.First().Student.StudentId,
                    FullName = g.First().Student.FullName,
                    QRToken = g.Key,
                    Age = g.First().Student.Age,
                    Count = g.Count()
                }).ToList();

            return studentsWithCount;
        }

        public async Task<StudentReportViewModel?> GetStudentReportAsync(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return null;

            var gradeLectures = await _context.LectureGrades
                .Where(lg => lg.GradeId == student.GradeId)
                .Include(lg => lg.Lecture)
                    .ThenInclude(l => l.Course)
                .Select(lg => lg.Lecture)
                .OrderByDescending(l => l.DateTime)
                .ToListAsync();

            var studentAttendances = await _context.StudentLectures
                .Where(sl => sl.StudentId == studentId)
                .ToDictionaryAsync(sl => sl.LectureId, sl => sl);

            var report = new StudentReportViewModel
            {
                Student = student,
                TotalLectures = gradeLectures.Count,
                LecturesDetails = gradeLectures.Select(l => new LectureAttendanceDetail
                {
                    LectureId = l.LectureId,
                    LectureTitle = l.Title,
                    CourseName = l.Course?.Name ?? "بدون مادة",
                    LectureDate = l.DateTime,
                    IsAttended = studentAttendances.ContainsKey(l.LectureId),
                    AttendedAt = studentAttendances.ContainsKey(l.LectureId) ? studentAttendances[l.LectureId].AttendedAt : null
                }).ToList()
            };

            report.AttendedCount = report.LecturesDetails.Count(l => l.IsAttended);
            report.AbsentCount = report.TotalLectures - report.AttendedCount;

            return report;
        }

        public async Task<Dictionary<int, double>> GetStudentsAttendancePercentagesAsync()
        {
            var lecturesPerGrade = await _context.LectureGrades
                .GroupBy(lg => lg.GradeId)
                .Select(g => new { GradeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GradeId, x => x.Count);

            var attendancePerStudent = await _context.StudentLectures
                .GroupBy(sl => sl.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StudentId, x => x.Count);

            // Global filter already scopes students to the current tenant
            var students = await _context.Students
                .Select(s => new { s.StudentId, s.GradeId })
                .ToListAsync();

            var percentages = new Dictionary<int, double>();
            foreach (var s in students)
            {
                int totalLectures = lecturesPerGrade.ContainsKey(s.GradeId) ? lecturesPerGrade[s.GradeId] : 0;
                int attended = attendancePerStudent.ContainsKey(s.StudentId) ? attendancePerStudent[s.StudentId] : 0;

                percentages[s.StudentId] = totalLectures == 0
                    ? 0
                    : Math.Round(((double)attended / totalLectures) * 100, 1);
            }

            return percentages;
        }

        public async Task<List<StudentReportItem>> GetComprehensiveReportAsync(int? gradeId)
        {
            var query = _context.Students
                .Include(s => s.Grade)
                .AsQueryable();

            if (gradeId.HasValue)
                query = query.Where(s => s.GradeId == gradeId.Value);

            var students = await query.ToListAsync();

            var lecturesPerGrade = await _context.LectureGrades
                .GroupBy(lg => lg.GradeId)
                .Select(g => new { GradeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GradeId, x => x.Count);

            var studentIds = students.Select(s => s.StudentId).ToList();

            var attendancePerStudent = await _context.StudentLectures
                .Where(sl => studentIds.Contains(sl.StudentId))
                .GroupBy(sl => sl.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StudentId, x => x.Count);

            // ─── Exam scores per student ──────────────────────────────────
            // For each student: sum their exam scores (only graded)
            var examScores = await _context.StudentExams
                .Where(se => studentIds.Contains(se.StudentId) && se.Score.HasValue)
                .GroupBy(se => se.StudentId)
                .Select(g => new { StudentId = g.Key, TotalScore = g.Sum(se => se.Score!.Value) })
                .ToDictionaryAsync(x => x.StudentId, x => x.TotalScore);

            // For each grade: sum MaxScore of all exams (to know the total possible)
            var gradeIds = students.Select(s => s.GradeId).Distinct().ToList();
            var examMaxPerGrade = await _context.Exams
                .Where(e => gradeIds.Contains(e.GradeId))
                .GroupBy(e => e.GradeId)
                .Select(g => new { GradeId = g.Key, MaxTotal = g.Sum(e => e.MaxScore) })
                .ToDictionaryAsync(x => x.GradeId, x => x.MaxTotal);

            var report = new List<StudentReportItem>();

            foreach (var student in students)
            {
                int totalLectures = lecturesPerGrade.ContainsKey(student.GradeId) ? lecturesPerGrade[student.GradeId] : 0;
                int attended = attendancePerStudent.ContainsKey(student.StudentId) ? attendancePerStudent[student.StudentId] : 0;
                int absent = Math.Max(0, totalLectures - attended);
                double percentage = totalLectures == 0 ? 0 : Math.Round(((double)attended / totalLectures) * 100, 1);
                double examTotal = examScores.ContainsKey(student.StudentId) ? examScores[student.StudentId] : 0;
                double examMax   = examMaxPerGrade.ContainsKey(student.GradeId) ? examMaxPerGrade[student.GradeId] : 0;

                report.Add(new StudentReportItem
                {
                    StudentId = student.StudentId,
                    FullName = student.FullName,
                    PhoneNumber = student.PhoneNumber,
                    QRToken = student.QRToken,
                    GradeId = student.GradeId,
                    GradeName = student.Grade?.Name ?? "غير محدد",
                    TotalLectures = totalLectures,
                    AttendedLectures = attended,
                    AbsentLectures = absent,
                    AttendancePercentage = percentage,
                    CalculatedScore = 0,   // set by controller after fetching grade marks
                    ExamTotalScore = examTotal,
                    ExamMaxScore = examMax
                });
            }

            return report.OrderBy(r => r.GradeName).ThenBy(r => r.FullName).ToList();
        }
    }
}
