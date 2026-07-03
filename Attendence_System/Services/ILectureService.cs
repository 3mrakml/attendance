using System.Collections.Generic;
using System.Threading.Tasks;
using Attendence_System.Models;
using Attendence_System.ViewModel;

namespace Attendence_System.Services
{
    public interface ILectureService
    {
        Task<List<Lecture>> GetLecturesByCourseAsync(int courseId);
        Task<Lecture> GetLectureByIdAsync(int id);
        Task<Lecture> CreateLectureAsync(Lecture lecture, List<int> gradeIds);
        Task<bool> CloseAttendanceAsync(int lectureId);
        Task<bool> DeleteLectureAsync(int lectureId);
        Task<Lecture?> UpdateLectureTitleAsync(int lectureId, string title);
        Task<List<Student>> GetStudentsInLectureAsync(int lectureId);

        Task<List<StudentAttendanceStatus>> GetStudentAttendanceStatusForLectureAsync(int lectureId);
        Task<List<Lecture>> GetFilteredLecturesAsync(string search, int? gradeId);
        Task SyncCountsAsync();
    }
}
