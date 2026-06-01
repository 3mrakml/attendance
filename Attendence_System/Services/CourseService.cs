using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Attendence_System.Data;
using Attendence_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Attendence_System.Services
{
    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;

        public CourseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Course>> GetCoursesByUserAsync(string userId)
        {
            return await _context.Courses
                .Include(c => c.CourseGrades)
                .ThenInclude(cg => cg.Grade)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<Course> GetCourseByIdAsync(int courseId)
        {
            return await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == courseId);
        }

        public async Task<Course> CreateCourseAsync(Course course, List<int> gradeIds)
        {
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            if (gradeIds != null && gradeIds.Any())
            {
                var courseGrades = gradeIds.Select(gradeId => new CourseGrade
                {
                    CourseId = course.CourseId,
                    GradeId = gradeId
                }).ToList();

                _context.CourseGrades.AddRange(courseGrades);
                await _context.SaveChangesAsync();
            }

            return course;
        }

        public async Task<bool> DeleteCourseAsync(int id, string userId)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null || course.UserId != userId)
            {
                return false;
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Course>> GetCoursesByGradeAndUserAsync(int gradeId, string userId)
        {
            return await _context.Courses
                .Where(c => c.UserId == userId && c.CourseGrades.Any(cg => cg.GradeId == gradeId))
                .ToListAsync();
        }

        public async Task<bool> IsCourseAssignedToGradeAsync(int courseId, int gradeId)
        {
            return await _context.CourseGrades
                .AnyAsync(cg => cg.CourseId == courseId && cg.GradeId == gradeId);
        }
    }
}
