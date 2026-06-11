using Attendence_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Attendence_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly string? _currentTenantId;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            _currentTenantId = httpContextAccessor?.HttpContext?.User?.FindFirstValue("TenantId");
        }

        // DbSets
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<StudentLecture> StudentLectures { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<CourseGrade> CourseGrades { get; set; }
        public DbSet<LectureGrade> LectureGrades { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<StudentExam> StudentExams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ─── Tenant ──────────────────────────────────────────────────────────
            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.Name)
                .IsUnique();

            // ─── AppUser → Tenant ─────────────────────────────────────────────
            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── SystemSetting ────────────────────────────────────────────────
            modelBuilder.Entity<SystemSetting>()
                .HasKey(ss => new { ss.TenantId, ss.Key });

            modelBuilder.Entity<SystemSetting>()
                .HasOne(ss => ss.Tenant)
                .WithMany()
                .HasForeignKey(ss => ss.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // ─── Grade → Tenant ───────────────────────────────────────────────
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Tenant)
                .WithMany(t => t.Grades)
                .HasForeignKey(g => g.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── Course → Tenant ──────────────────────────────────────────────
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Tenant)
                .WithMany(t => t.Courses)
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── Student → Tenant ─────────────────────────────────────────────
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Tenant)
                .WithMany(t => t.Students)
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── Student → Grade ──────────────────────────────────────────────
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Grade)
                .WithMany(g => g.Students)
                .HasForeignKey(s => s.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique Index on QRToken
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.QRToken)
                .IsUnique();

            // ─── CourseGrade (Many-to-Many) ───────────────────────────────────
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

            // ─── Lecture → Course ─────────────────────────────────────────────
            modelBuilder.Entity<Lecture>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lectures)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ─── LectureGrade (Many-to-Many) ──────────────────────────────────
            modelBuilder.Entity<LectureGrade>()
                .HasKey(lg => new { lg.LectureId, lg.GradeId });

            modelBuilder.Entity<LectureGrade>()
                .HasOne(lg => lg.Lecture)
                .WithMany(l => l.LectureGrades)
                .HasForeignKey(lg => lg.LectureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LectureGrade>()
                .HasOne(lg => lg.Grade)
                .WithMany(g => g.LectureGrades)
                .HasForeignKey(lg => lg.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── StudentLecture ───────────────────────────────────────────────
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

            modelBuilder.Entity<StudentLecture>()
                .Property(sl => sl.AttendedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ─── Exam → Course ────────────────────────────────────────────────
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ─── Exam → Grade ─────────────────────────────────────────────────
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Grade)
                .WithMany()
                .HasForeignKey(e => e.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── Exam → Tenant ────────────────────────────────────────────────
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── StudentExam ──────────────────────────────────────────────────
            modelBuilder.Entity<StudentExam>()
                .HasOne(se => se.Student)
                .WithMany()
                .HasForeignKey(se => se.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentExam>()
                .HasOne(se => se.Exam)
                .WithMany(e => e.StudentExams)
                .HasForeignKey(se => se.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentExam>()
                .HasIndex(se => new { se.StudentId, se.ExamId })
                .IsUnique();

            // ─── Identity Tables ──────────────────────────────────────────────
            modelBuilder.Entity<AppUser>().ToTable("Users", "security");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles", "security");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "security");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "security");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "security");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "security");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "security");

            // ─── GLOBAL QUERY FILTERS (Automatic Tenant Isolation) ───────────
            // These filters are applied automatically to ALL queries.
            // No need to add .Where(x => x.TenantId == ...) anywhere in the code.
            if (_httpContextAccessor != null)
            {
                modelBuilder.Entity<Grade>().HasQueryFilter(g =>
                    _httpContextAccessor.HttpContext == null ||
                    _httpContextAccessor.HttpContext.User == null ||
                    g.TenantId == _currentTenantId);

                modelBuilder.Entity<Course>().HasQueryFilter(c =>
                    _httpContextAccessor.HttpContext == null ||
                    _httpContextAccessor.HttpContext.User == null ||
                    c.TenantId == _currentTenantId);

                modelBuilder.Entity<Student>().HasQueryFilter(s =>
                    _httpContextAccessor.HttpContext == null ||
                    _httpContextAccessor.HttpContext.User == null ||
                    s.TenantId == _currentTenantId);

                modelBuilder.Entity<SystemSetting>().HasQueryFilter(ss =>
                    _httpContextAccessor.HttpContext == null ||
                    _httpContextAccessor.HttpContext.User == null ||
                    ss.TenantId == _currentTenantId);

                modelBuilder.Entity<Exam>().HasQueryFilter(e =>
                    _httpContextAccessor.HttpContext == null ||
                    _httpContextAccessor.HttpContext.User == null ||
                    e.TenantId == _currentTenantId);
            }
        }
    }
}
