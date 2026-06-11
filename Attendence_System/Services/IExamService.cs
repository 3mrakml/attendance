using Attendence_System.Models;

namespace Attendence_System.Services
{
    public interface IExamService
    {
        Task<List<Exam>> GetAllExamsAsync();
        Task<Exam?> GetExamWithStudentsAsync(int examId);
        Task CreateExamAsync(Exam exam, string tenantId);
        Task<List<StudentExam>> GetOrCreateStudentExamsAsync(int examId);
        Task SaveStudentScoresAsync(int examId, Dictionary<int, double?> scores);
        Task DeleteExamAsync(int examId);
        Task<List<StudentExam>> GetStudentExamsAsync(int studentId);
    }
}
