using System.ComponentModel.DataAnnotations;

namespace Attendence_System.Models
{
    public class Tenant
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public ICollection<Course> Courses { get; set; } = new List<Course>();
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
