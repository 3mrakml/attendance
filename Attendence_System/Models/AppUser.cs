using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Attendence_System.Models
{
    public class AppUser : IdentityUser
    {
        [Required,MaxLength(100)]
        public string Name { get; set; }

        public ICollection<Course> Courses { get; set; } = new List<Course>();


    }

}
