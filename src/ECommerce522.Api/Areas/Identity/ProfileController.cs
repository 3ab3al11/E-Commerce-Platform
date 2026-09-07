using ECommerce522.APIV9.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce522.APIV9.Areas.Identity
{
    [Route("[area]/[controller]")]
    [ApiController]
    [Area("Identity")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            return Ok(user.Adapt<ApplicationUserResponse>());
        }

        [HttpPost("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile(ApplicationUserRequest applicationUserRequest)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            user.Name = applicationUserRequest.Name;
            user.UserName = applicationUserRequest.UserName;
            user.Email = applicationUserRequest.Email;
            user.PhoneNumber = applicationUserRequest.PhoneNumber;
            user.Address = applicationUserRequest.Address;

            await _userManager.UpdateAsync(user);

            return NoContent();
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(ApplicationUserRequest applicationUserRequest)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, applicationUserRequest.OldPassword, applicationUserRequest.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }
    }
}
