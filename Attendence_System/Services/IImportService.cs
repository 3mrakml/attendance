namespace Attendence_System.Services
{
    public class ImportStudentResult
    {
        public int AddedCount { get; set; }
        public List<ImportStudentError> Errors { get; set; } = new();
        public bool HasErrors => Errors.Any();
    }

    public class ImportStudentError
    {
        /// <summary>اسم الطالب أو "الصف رقم X" إذا كان الاسم فارغاً</summary>
        public string Label { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public interface IImportService
    {
        Task<ImportStudentResult> ImportStudentsFromExcelAsync(IFormFile file, string tenantId);
        byte[] GenerateStudentTemplate(IEnumerable<string> gradeNames);
    }
}
