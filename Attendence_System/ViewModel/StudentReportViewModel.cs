using System;
using System.Collections.Generic;
using Attendence_System.Models;

namespace Attendence_System.ViewModel
{
    public class StudentReportViewModel
    {
        public Student Student { get; set; }
        
        public int TotalLectures { get; set; }
        public int AttendedCount { get; set; }
        public int AbsentCount { get; set; }
        
        public double AttendancePercentage => TotalLectures > 0 
            ? Math.Round(((double)AttendedCount / TotalLectures) * 100, 1) 
            : 0;

        public List<LectureAttendanceDetail> LecturesDetails { get; set; } = new List<LectureAttendanceDetail>();
    }

    public class LectureAttendanceDetail
    {
        public int LectureId { get; set; }
        public string LectureTitle { get; set; }
        public string CourseName { get; set; }
        public DateTime LectureDate { get; set; }
        public bool IsAttended { get; set; }
        public DateTime? AttendedAt { get; set; }
    }
}
