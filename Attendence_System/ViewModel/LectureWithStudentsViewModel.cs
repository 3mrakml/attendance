using Attendence_System.Models;
using System;
using System.Collections.Generic;

namespace Attendence_System.ViewModel
{
    public class LectureWithStudentsViewModel
    {
        public int lectureid { get; set; }
        public string LectureTitle { get; set; }
        
        public int TotalStudents { get; set; }
        public int AttendedCount { get; set; }
        public int AbsentCount { get; set; }

        public List<StudentAttendanceStatus> Students { get; set; } = new List<StudentAttendanceStatus>();
    }

    public class StudentAttendanceStatus
    {
        public Student Student { get; set; }
        public bool IsAttended { get; set; }
        public DateTime? AttendedAt { get; set; }
    }
}
