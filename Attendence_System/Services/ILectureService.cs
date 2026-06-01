using System.Collections.Generic;
using System.Threading.Tasks;
using Attendence_System.Models;

namespace Attendence_System.Services
{
    public interface ILectureService
    {
        Task<List<Lecture>> GetLecturesByCourseAsync(int courseId);
        Task<Lecture> GetLectureByIdAsync(int id);
        Task<Lecture> CreateLectureAsync(Lecture lecture);
        Task<bool> CloseAttendanceAsync(int lectureId);
        Task<bool> DeleteLectureAsync(int lectureId);
        Task<List<Student>> GetStudentsInLectureAsync(int lectureId);
    }
}
