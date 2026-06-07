using Attendence_System.Data;
using Attendence_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Attendence_System.Services;

var builder = WebApplication.CreateBuilder(args);

// Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// DbContext — inject IHttpContextAccessor for Global Query Filters
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
    options.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSingleton<System.TimeProvider>(System.TimeProvider.System);

// Identity
builder.Services.AddIdentityCore<AppUser>(options =>
{
    // Allow same email across different tenants (uniqueness enforced per tenant in code)
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddSignInManager()
.AddDefaultTokenProviders()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Cookie Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddCookie(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
})
.AddCookie(IdentityConstants.ExternalScheme);

builder.Services.AddControllersWithViews();

// Application Services
builder.Services.AddScoped<IQRCodeService, QRCodeService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ILectureService, LectureService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();
builder.Services.AddScoped<IImportService, ImportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Lecture}/{action=Create}/{id?}");

app.Run();