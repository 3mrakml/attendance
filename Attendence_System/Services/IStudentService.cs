using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Attendence_System.Models;
using Attendence_System.ViewModel;

namespace Attendence_System.Services
{
    public interface IStudentService
    {
        Task<List<Student>> GetAllStudentsAsync();
        Task<Student> GetStudentByIdAsync(int id);
        Task<Student> GetStudentByIdCollegeAsync(string idCollege);
        Task<bool> StudentExistsAsync(string idCollege);
        Task<Student> CreateStudentAsync(Student student);
        Task<bool> UpdateStudentAsync(Student student);
        Task<bool> DeleteStudentAsync(int id);
        
        // Attendance logic
        Task<(bool Success, string Message)> RegisterAttendanceAsync(ScanRequestVM request);
        Task<List<StudentWithCount>> GetCourseAttendanceStatsAsync(int courseId);
        Task<Dictionary<int, double>> GetStudentsAttendancePercentagesAsync();
        Task<StudentReportViewModel> GetStudentReportAsync(int studentId);
    }
}
