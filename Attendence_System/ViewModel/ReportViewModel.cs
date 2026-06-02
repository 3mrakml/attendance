using System.Collections.Generic;

namespace Attendence_System.ViewModel
{
    public class ComprehensiveReportViewModel
    {
        public List<GradeMarksViewModel> GradeSettings { get; set; } = new List<GradeMarksViewModel>();
        public List<StudentReportItem> Students { get; set; } = new List<StudentReportItem>();
        public int? SelectedGradeId { get; set; }
    }

    public class GradeMarksViewModel
    {
        public int GradeId { get; set; }
        public string GradeName { get; set; }
        public double MaxMarks { get; set; }
    }

    public class StudentReportItem
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string QRToken { get; set; }
        public string GradeName { get; set; }
        public int GradeId { get; set; }
        public int TotalLectures { get; set; }
        public int AttendedLectures { get; set; }
        public int AbsentLectures { get; set; }
        public double AttendancePercentage { get; set; }
        public double CalculatedScore { get; set; }
    }
}
