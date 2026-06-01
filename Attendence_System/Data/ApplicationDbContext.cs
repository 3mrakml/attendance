using Attendence_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Attendence_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets لكل نموذج تم إنشاؤه
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<StudentLecture> StudentLectures { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<CourseGrade> CourseGrades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // إعداد العلاقة بين المادة والصف (Many-to-Many)
            modelBuilder.Entity<CourseGrade>()
                .HasKey(cg => new { cg.CourseId, cg.GradeId });

            modelBuilder.Entity<CourseGrade>()
                .HasOne(cg => cg.Course)
                .WithMany(c => c.CourseGrades)
                .HasForeignKey(cg => cg.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseGrade>()
                .HasOne(cg => cg.Grade)
                .WithMany(g => g.CourseGrades)
                .HasForeignKey(cg => cg.GradeId)
                .OnDelete(DeleteBehavior.Cascade);

            // إعداد العلاقة بين الطالب والصف (One-to-Many)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Grade)
                .WithMany(g => g.Students)
                .HasForeignKey(s => s.GradeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique Index على QRToken لمنع التكرار (1NF)
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.QRToken)
                .IsUnique();

            // عند حذف الكورس سيتم حذف جميع المحاضرات المرتبطة به
            modelBuilder.Entity<Lecture>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lectures)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ربط المحاضرة بالصف
            modelBuilder.Entity<Lecture>()
                .HasOne(l => l.Grade)
                .WithMany(g => g.Lectures)
                .HasForeignKey(l => l.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            // جدول الحضور - Composite PK (StudentId + LectureId) فقط (2NF)
            modelBuilder.Entity<StudentLecture>()
                .HasKey(sl => new { sl.StudentId, sl.LectureId });

            modelBuilder.Entity<StudentLecture>()
                .HasOne(sl => sl.Student)
                .WithMany(s => s.StudentLectures)
                .HasForeignKey(sl => sl.StudentId);

            modelBuilder.Entity<StudentLecture>()
                .HasOne(sl => sl.Lecture)
                .WithMany(l => l.StudentLectures)
                .HasForeignKey(sl => sl.LectureId)
                .OnDelete(DeleteBehavior.Cascade);

            // قيمة افتراضية لـ AttendedAt
            modelBuilder.Entity<StudentLecture>()
                .Property(sl => sl.AttendedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // تكوين جداول Identity
            modelBuilder.Entity<AppUser>().ToTable("Users", "security");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles", "security");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "security");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "security");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "security");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "security");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "security");
        }

    }
}
