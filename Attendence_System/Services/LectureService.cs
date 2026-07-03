using Attendence_System.Data;
using Attendence_System.Models;
using Attendence_System.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Attendence_System.Services
{
    public class LectureService : ILectureService
    {
        private readonly ApplicationDbContext _context;
        private readonly IQRCodeService _qrCodeService;

        public LectureService(ApplicationDbContext context, IQRCodeService qrCodeService)
        {
            _context = context;
            _qrCodeService = qrCodeService;
        }

        public async Task<List<Lecture>> GetLecturesByCourseAsync(int courseId)
        {
            return await _context.Lectures
                .AsNoTracking()
                .Include(l => l.LectureGrades)
                    .ThenInclude(lg => lg.Grade)
                .Where(l => l.CourseId == courseId)
                .ToListAsync();
        }

        public async Task<Lecture?> GetLectureByIdAsync(int id)
        {
            return await _context.Lectures
                .Include(l => l.Course)
                .Include(l => l.LectureGrades)
                    .ThenInclude(lg => lg.Grade)
                .FirstOrDefaultAsync(l => l.LectureId == id);
        }

        public async Task<Lecture> CreateLectureAsync(Lecture lecture, List<int> gradeIds)
        {
            if (gradeIds != null && gradeIds.Any())
            {
                lecture.LectureGrades = gradeIds.Select(gId => new LectureGrade { GradeId = gId }).ToList();
            }

            _context.Lectures.Add(lecture);
            await _context.SaveChangesAsync();

            return lecture;
        }

        public async Task<bool> CloseAttendanceAsync(int lectureId)
        {
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture != null)
            {
                lecture.IsAttendanceClosed = true;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<Lecture?> UpdateLectureTitleAsync(int lectureId, string title)
        {
            var lecture = await _context.Lectures.FindAsync(lectureId);

            if (lecture == null) return null;

            lecture.Title = title;

            await _context.SaveChangesAsync();
            return lecture;
        }

        public async Task<bool> DeleteLectureAsync(int lectureId)
        {
            var deleted = await _context.Lectures
                .Where(l => l.LectureId == lectureId)
                .ExecuteDeleteAsync();

            return deleted > 0;
        }

        public async Task<List<Student>> GetStudentsInLectureAsync(int lectureId)
        {
            return await _context.StudentLectures
                .Where(sl => sl.LectureId == lectureId)
                .Select(sl => sl.Student)
                .ToListAsync();
        }



        public async Task<List<StudentAttendanceStatus>> GetStudentAttendanceStatusForLectureAsync(int lectureId)
        {
            var lecture = await _context.Lectures
                .Include(l => l.LectureGrades)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LectureId == lectureId);

            if (lecture == null) return new List<StudentAttendanceStatus>();

            var gradeIds = lecture.LectureGrades.Select(lg => lg.GradeId).ToList();

            var query = from student in _context.Students.Where(s => gradeIds.Contains(s.GradeId)).Include(s => s.Grade)
                        join sl in _context.StudentLectures.Where(sl => sl.LectureId == lectureId)
                        on student.StudentId equals sl.StudentId into slGroup
                        from slRecord in slGroup.DefaultIfEmpty()
                        select new StudentAttendanceStatus
                        {
                            Student = student,
                            IsAttended = slRecord != null,
                            AttendedAt = slRecord != null ? slRecord.AttendedAt : (DateTime?)null
                        };

            var list = await query.ToListAsync();

            return list.OrderByDescending(s => s.IsAttended).ThenBy(s => s.Student.FullName).ToList();
        }


        public async Task<List<Lecture>> GetFilteredLecturesAsync(string search, int? gradeId)
        {
            var query = _context.Lectures.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l =>
                    l.Title.Contains(search) ||
                    l.Course.Name.Contains(search));
            }

            if (gradeId.HasValue)
            {
                query = query.Where(l =>
                    l.LectureGrades.Any(lg => lg.GradeId == gradeId.Value));
            }

            // Project only the exact columns needed to prevent massive data fetch (e.g. bypassing QRCode blobs)
            var projected = await query
                .OrderByDescending(l => l.DateTime)
                .Select(l => new 
                {
                    l.LectureId,
                    l.Title,
                    l.DateTime,
                    l.AttendedCount,
                    CourseName = l.Course.Name,
                    Grades = l.LectureGrades.Select(lg => new { lg.Grade.Name, lg.Grade.StudentCount }).ToList()
                })
                .ToListAsync();

            // Reconstruct minimal Lecture objects for the View
            var list = projected.Select(p => new Lecture
            {
                LectureId = p.LectureId,
                Title = p.Title,
                DateTime = p.DateTime,
                AttendedCount = p.AttendedCount,
                Course = new Course { Name = p.CourseName },
                LectureGrades = p.Grades.Select(g => new LectureGrade 
                {
                    Grade = new Grade { Name = g.Name, StudentCount = g.StudentCount }
                }).ToList()
            }).ToList();

            return list;
        }

        public async Task SyncCountsAsync()
        {
            // تحديث كل الصفوف باستعلام واحد فقط في قاعدة البيانات
            await _context.Grades
                .ExecuteUpdateAsync(g => g.SetProperty(
                    grade => grade.StudentCount, 
                    grade => grade.Students.Count()));

            // تحديث كل المحاضرات باستعلام واحد فقط في قاعدة البيانات
            await _context.Lectures
                .ExecuteUpdateAsync(l => l.SetProperty(
                    lecture => lecture.AttendedCount, 
                    lecture => lecture.StudentLectures.Count()));
        }
    }
}
