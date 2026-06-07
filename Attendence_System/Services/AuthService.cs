using Attendence_System.Data;
using Attendence_System.Models;
using Attendence_System.ViewModel;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Attendence_System.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task<SignInResult> LoginAsync(LoginViewModel model)
        {
            // Find by email — search by username (email) since UserName == Email
            var user = await _userManager.FindByNameAsync(model.Email)
                    ?? await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return SignInResult.Failed;

            // Sign in and include TenantId as an extra claim
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded && user.TenantId != null)
            {
                // Add TenantId claim so DbContext Global Query Filters work automatically
                var additionalClaims = new List<Claim>
                {
                    new Claim("TenantId", user.TenantId)
                };
                await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, additionalClaims);
            }

            return result;
        }

        public async Task<(IdentityResult Result, AppUser User)> RegisterAsync(RegisterViewModel model)
        {
            // 1. Create a new Tenant for this teacher
            var tenant = new Tenant
            {
                Name = model.FirstName + " " + model.LastName
            };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // 2. Create the user linked to the new Tenant
            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.FirstName + " " + model.LastName,
                TenantId = tenant.Id
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                // Rollback: remove the tenant if user creation fails
                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync();
            }

            return (result, user);
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
