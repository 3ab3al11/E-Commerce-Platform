using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce522.APIV9.Utilities.DBInitializer
{
    public class DBInitializer : IDBInitializer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public DBInitializer(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
        }

        public void Initialize()
        {
            if (_context.Database.GetPendingMigrations().Any())
            {
                _context.Database.Migrate();
            }

            string[] roles = [SD.SUPER_ADMIN_ROLE, SD.ADMIN_ROLE, SD.EMPLOYEE_ROLE, SD.CUSTOMER_ROLE];
            foreach (var role in roles)
            {
                if (!_roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
                    _roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            }

            var adminEmail = _configuration["SeedAdmin:Email"];
            var adminPassword = _configuration["SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword)) return;

            var user = _userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Name = _configuration["SeedAdmin:Name"] ?? "SuperAdmin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    UserName = _configuration["SeedAdmin:UserName"] ?? "SuperAdmin"
                };
                var result = _userManager.CreateAsync(user, adminPassword).GetAwaiter().GetResult();
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            if (!_userManager.IsInRoleAsync(user, SD.SUPER_ADMIN_ROLE).GetAwaiter().GetResult())
                _userManager.AddToRoleAsync(user, SD.SUPER_ADMIN_ROLE).GetAwaiter().GetResult();
        }
    }
}
