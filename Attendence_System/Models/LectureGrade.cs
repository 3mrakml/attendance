namespace Attendence_System.Models
{
    /// <summary>
    /// جدول وسيط لعلاقة Many-to-Many بين المحاضرة والصف الدراسي
    /// يتيح ربط محاضرة واحدة بأكثر من مستوى/صف
    /// </summary>
    public class LectureGrade : IMustHaveTenant
    {
        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public int LectureId { get; set; }
        public Lecture Lecture { get; set; }

        public int GradeId { get; set; }
        public Grade Grade { get; set; }
    }
}
