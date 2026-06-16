using Attendence_System.Data;
using Attendence_System.Models;
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
            _context.Lectures.Add(lecture);
            await _context.SaveChangesAsync();

            foreach (var gradeId in gradeIds)
            {
                _context.LectureGrades.Add(new LectureGrade
                {
                    LectureId = lecture.LectureId,
                    GradeId = gradeId
                });
            }
            await _context.SaveChangesAsync();

            lecture.QRCode = _qrCodeService.GenerateQRCode(lecture.LectureId.ToString());
            _context.Lectures.Update(lecture);
            await _context.SaveChangesAsync();

            return lecture;
        }

        public async Task<bool> CloseAttendanceAsync(int lectureId)
        {
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture != null)
            {
                lecture.IsAttendanceClosed = true;
                _context.Lectures.Update(lecture);
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
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture == null) return false;

            _context.Lectures.Remove(lecture);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Student>> GetStudentsInLectureAsync(int lectureId)
        {
            return await _context.StudentLectures
                .Where(sl => sl.LectureId == lectureId)
                .Include(sl => sl.Student)
                .Select(sl => sl.Student)
                .ToListAsync();
        }

        public System.Linq.IQueryable<Lecture> GetAllLecturesQueryable()
        {
            return _context.Lectures
                .AsNoTracking()
                .Include(l => l.Course)
                .Include(l => l.StudentLectures)
                .Include(l => l.LectureGrades)
                    .ThenInclude(lg => lg.Grade)
                .OrderByDescending(l => l.DateTime)
                .AsQueryable();
        }
    }
}
