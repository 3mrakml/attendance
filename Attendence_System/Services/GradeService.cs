using Attendence_System.Data;
using Attendence_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Attendence_System.Services
{
    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _context;

        public GradeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Grade>> GetAllGradesAsync()
        {
            return await _context.Grades
                .Include(g => g.CourseGrades)
                .Include(g => g.Students)
                .ToListAsync();
        }

        public async Task<Grade> GetGradeByIdAsync(int id)
        {
            return await _context.Grades
                .Include(g => g.CourseGrades)
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.GradeId == id);
        }

        public async Task<bool> AddGradeAsync(Grade grade)
        {
            try
            {
                _context.Grades.Add(grade);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateGradeAsync(Grade grade)
        {
            try
            {
                _context.Grades.Update(grade);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteGradeAsync(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null) return false;

            try
            {
                _context.Grades.Remove(grade);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
