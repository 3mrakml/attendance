using Attendence_System.Models;

namespace Attendence_System.Services
{
    public interface IGradeService
    {
        Task<List<Grade>> GetAllGradesAsync();
        Task<Grade?> GetGradeByIdAsync(int id);
        Task<bool> AddGradeAsync(Grade grade);
        Task<bool> UpdateGradeAsync(Grade grade);
        Task<bool> DeleteGradeAsync(int id);
    }
}
