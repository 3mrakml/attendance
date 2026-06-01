using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            return await _context.Students
                .Include(s => s.Grade)
                .ToListAsync();
        }

        public async Task<Student> GetStudentByIdAsync(int id)
        {
            return await _context.Students.FindAsync(id);
        }

        public async Task<Student> GetStudentByIdCollegeAsync(string qrToken)
        {
            return await _context.Students.FirstOrDefaultAsync(s => s.QRToken == qrToken);
        }

        public async Task<bool> StudentExistsAsync(string qrToken)
        {
            return await _context.Students.AnyAsync(s => s.QRToken == qrToken);
        }

        public async Task<Student> CreateStudentAsync(Student student)
        {
            // لا يوجد AttendenceNumber - يُحسب ديناميكياً من StudentLectures (3NF fix)
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> RegisterAttendanceAsync(ScanRequestVM request)
        {
            var lecture = await _context.Lectures.FindAsync(request.LectureId);
            if (lecture == null)
                return (false, "Lecture not found.");

            if (lecture.IsAttendanceClosed)
                return (false, "Attendance is closed for this lecture.");

            var student = await GetStudentByIdCollegeAsync(request.IdCollege);
            if (student == null)
                return (false, $"Student with QR Token {request.IdCollege} not found.");

            // التحقق من أن الطالب ينتمي للصف المخصص للمحاضرة (Grade validation)
            if (student.GradeId != lecture.GradeId)
                return (false, $"الطالب ({student.FullName}) غير مسجل في الصف المخصص لهذه المحاضرة.");

            bool isAlreadyRegistered = await _context.StudentLectures
                .AnyAsync(sl => sl.StudentId == student.StudentId && sl.LectureId == lecture.LectureId);

            if (isAlreadyRegistered)
                return (false, $"{student.FullName} سجّل حضوره بالفعل في هذه المحاضرة.");

            _context.StudentLectures.Add(new StudentLecture
            {
                StudentId = student.StudentId,
                LectureId = lecture.LectureId,
                AttendedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return (true, $"{student.FullName} تم تسجيل حضوره بنجاح!");
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

        public async Task<StudentReportViewModel> GetStudentReportAsync(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return null;

            // Get all lectures assigned to this student's grade
            var gradeLectures = await _context.Lectures
                .Include(l => l.Course)
                .Where(l => l.GradeId == student.GradeId)
                .OrderByDescending(l => l.DateTime)
                .ToListAsync();

            // Get the student's attendance records
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
            // 1. Get total lectures per grade
            var lecturesPerGrade = await _context.Lectures
                .GroupBy(l => l.GradeId)
                .Select(g => new { GradeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GradeId, x => x.Count);

            // 2. Get total attended lectures per student
            var attendancePerStudent = await _context.StudentLectures
                .GroupBy(sl => sl.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StudentId, x => x.Count);

            // 3. Get students and their grades
            var students = await _context.Students
                .Select(s => new { s.StudentId, s.GradeId })
                .ToListAsync();

            var percentages = new Dictionary<int, double>();
            foreach(var s in students)
            {
                int totalLectures = lecturesPerGrade.ContainsKey(s.GradeId) 
                    ? lecturesPerGrade[s.GradeId] : 0;
                
                int attended = attendancePerStudent.ContainsKey(s.StudentId) 
                    ? attendancePerStudent[s.StudentId] : 0;

                if (totalLectures == 0)
                    percentages[s.StudentId] = 0;
                else
                    percentages[s.StudentId] = Math.Round(((double)attended / totalLectures) * 100, 1);
            }

            return percentages;
        }
    }
}
