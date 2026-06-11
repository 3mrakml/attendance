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
        /// <summary>Attendance score out of MaxMarks (configurable per grade)</summary>
        public double CalculatedScore { get; set; }
        /// <summary>Sum of all exam scores for this student</summary>
        public double ExamTotalScore { get; set; }
        /// <summary>Sum of MaxScore across all exams for this student's grade</summary>
        public double ExamMaxScore { get; set; }
        /// <summary>Attendance score + exam total score</summary>
        public double TotalGrade => Math.Round(CalculatedScore + ExamTotalScore, 2);
    }
}
