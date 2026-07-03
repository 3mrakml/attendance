using Attendence_System.Models;
using Attendence_System.ViewModel;

namespace Attendence_System.Services
{
    public interface IStudentService
    {
        Task<List<Student>> GetAllStudentsAsync();
        Task<Student?> GetStudentByIdAsync(int id);
        Task<Student?> GetStudentByIdCollegeAsync(string qrToken);
        Task<Student?> GetStudentByPhoneNumberAsync(string phoneNumber);
        Task<bool> StudentExistsAsync(string qrToken);
        Task<Student> CreateStudentAsync(Student student);
        Task<bool> UpdateStudentAsync(Student student);
        Task<bool> DeleteStudentAsync(int id);
        Task<string> GenerateSequentialQRTokenAsync(int gradeId);

        // Attendance logic
        Task<(bool Success, string Message)> RegisterAttendanceAsync(ScanRequestVM request);
        Task<List<StudentWithCount>> GetCourseAttendanceStatsAsync(int courseId);
        Task<Dictionary<int, double>> GetStudentsAttendancePercentagesAsync();
        Task<StudentReportViewModel?> GetStudentReportAsync(int studentId);
        Task<List<StudentReportItem>> GetComprehensiveReportAsync(int? gradeId);
        Task<Dictionary<int, int>> GetStudentCountByGradeAsync();
        Task<Dictionary<int, int>> GetStudentCountByGradeIdsAsync(List<int> gradeIds);
    }
}
