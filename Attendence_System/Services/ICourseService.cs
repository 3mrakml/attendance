using Attendence_System.Models;

namespace Attendence_System.Services
{
    public interface ICourseService
    {
        Task<List<Course>> GetAllCoursesAsync();
        Task<Course?> GetCourseByIdAsync(int courseId);
        Task<Course> CreateCourseAsync(Course course, List<int> gradeIds);
        Task<Course?> UpdateCourseAsync(int courseId, string name, List<int> gradeIds);
        Task<bool> DeleteCourseAsync(int id);
        Task<List<Course>> GetCoursesByGradeAsync(int gradeId);
        Task<bool> IsCourseAssignedToGradeAsync(int courseId, int gradeId);
        Task<List<Course>> GetCommonCoursesByGradesAsync(List<int> gradeIds);
        Task<bool> AreCoursesAssignedToGradesAsync(int courseId, List<int> gradeIds);
    }
}
