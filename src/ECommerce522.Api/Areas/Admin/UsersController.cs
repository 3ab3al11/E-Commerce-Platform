using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce522.APIV9.Areas.Admin
{
    [Route("api/[Area]/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE}")]
    [Area("Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var users = _userManager.Users.AsNoTracking().AsQueryable();

            return Ok(users.AsEnumerable());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Lock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user is null)
                return NotFound();

            user.LockoutEnabled = false;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddDays(30);

            await _userManager.UpdateAsync(user);

            return NoContent();
        }
    }
}
