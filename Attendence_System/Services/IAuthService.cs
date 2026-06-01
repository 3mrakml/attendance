using Attendence_System.Models;
using Attendence_System.ViewModel;
using Microsoft.AspNetCore.Identity;

namespace Attendence_System.Services
{
    public interface IAuthService
    {
        Task<SignInResult> LoginAsync(LoginViewModel model);
        Task<(IdentityResult Result, AppUser User)> RegisterAsync(RegisterViewModel model);
        Task LogoutAsync();
    }
}
