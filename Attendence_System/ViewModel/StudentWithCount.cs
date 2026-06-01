namespace Attendence_System.ViewModel
{
    public class StudentWithCount
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public string QRToken { get; set; }
        public int Age { get; set; }
        public int Count { get; set; }  // عدد المحاضرات التي حضرها الطالب
    }
}
