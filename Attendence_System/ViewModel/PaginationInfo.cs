namespace Attendence_System.ViewModel
{
    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public Dictionary<string, string> RouteParams { get; set; } = new Dictionary<string, string>();
    }
}
