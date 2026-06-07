using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Attendence_System.Models
{
    public class AppUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        // Multi-tenancy: each teacher belongs to ONE Tenant
        public string? TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
