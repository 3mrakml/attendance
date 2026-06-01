using System.Collections.Generic;
using System.Threading.Tasks;
using Attendence_System.Models;

namespace Attendence_System.Services
{
    public interface ICourseService
    {
        Task<List<Course>> GetCoursesByUserAsync(string userId);
        Task<Course> GetCourseByIdAsync(int courseId);
        Task<Course> CreateCourseAsync(Course course, List<int> gradeIds);
        Task<bool> DeleteCourseAsync(int id, string userId);
        Task<List<Course>> GetCoursesByGradeAndUserAsync(int gradeId, string userId);
        Task<bool> IsCourseAssignedToGradeAsync(int courseId, int gradeId);
    }
}
