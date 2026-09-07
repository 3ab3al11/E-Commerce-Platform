using ECommerce522.APIV9.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce522.APIV9.Areas.Identity
{
    [Route("auth/[area]/[controller]")]
    [ApiController]
    [Area("Identity")]
    public class AccountController : ControllerBase
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<ApplicationUserOTP> _applicationUserOTPRepository;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ITokenService _tokenService;
        private readonly IStringLocalizer<LocalizationController> _localizer;

        public AccountController(UserManager<ApplicationUser> userManager, IRepository<ApplicationUserOTP> applicationUserOTPRepository, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, ITokenService tokenService, IStringLocalizer<LocalizationController> localizer)
        {
            _userManager = userManager;
            _applicationUserOTPRepository = applicationUserOTPRepository;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _tokenService = tokenService;
            _localizer = localizer;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterRequest registerRequest)
        {
            // Create User In DB
            ApplicationUser user = new()
            {
                Email = registerRequest.Email,
                UserName = registerRequest.UserName,
                Name = registerRequest.Name,
                Address = registerRequest.Address,
            };

            //ApplicationUser user = registerRequest.Adapt<ApplicationUser>();

            var result = await _userManager.CreateAsync(user, registerRequest.Password);

            //ModelStateDictionary keyValuePairs = new();

            //if (!result.Succeeded)
            //    foreach (var item in result.Errors)
            //    {
            //        keyValuePairs.AddModelError(string.Empty, item);
            //        return BadRequest(keyValuePairs);
            //    }

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Send Email Confirmation
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action("Confirm", "Account", new { area = "Identity", token = token, userId = user.Id }, Request.Scheme);

            await _emailSender.SendEmailAsync(registerRequest.Email,
                "Ecommerce522 - Confirm Your Email",
                $"<h1>Please Confirm Your Email By Clicking <a href='{link}'>Here</a>.</h1>"
                );

            // Save User in his role
            await _userManager.AddToRoleAsync(user, SD.CUSTOMER_ROLE);

            //return Ok(new
            //{
            //    success_notification = "Add Account Successfully, Please Confirm Your Account"
            //});

            return Created($"{Request.Scheme}://{Request.Host}/Identity/Account/Login", new
            {
                //success_notification = "Add Account Successfully, Please Confirm Your Account"
                success_notification = _localizer["RegisterNewAccount"].Value
            });
        }

        [HttpGet("Confirm")]
        public async Task<IActionResult> Confirm(string token, string userId)
        {

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
                return BadRequest(result.Errors);
            else
                return Ok(new
                {
                    success_notification = "Confirm Account Successfully"
                });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            var user = await _userManager.FindByEmailAsync(loginRequest.EmailOrUserName) ?? await _userManager.FindByNameAsync(loginRequest.EmailOrUserName);

            if (user is null)
                return NotFound(new
                {
                    error_notification = "Invalid User Name / Email OR Password"
                });

            var result = await _signInManager.PasswordSignInAsync(user, loginRequest.Password, lockoutOnFailure: true, isPersistent: loginRequest.RememberMe);

            ModelStateDictionary keyValuePairs = new();
            if (!result.Succeeded)
            {
                if (!user.LockoutEnabled)
                    keyValuePairs.AddModelError(string.Empty, $"Locked User till {user.LockoutEnd}");

                if (result.IsNotAllowed)
                    keyValuePairs.AddModelError(string.Empty, "Invalid User Name / Email OR Password");
                else if (result.IsLockedOut)
                {
                    // Send Email
                    keyValuePairs.AddModelError(string.Empty, "Too Many Attempts, Please Try Later");
                }

                return BadRequest(keyValuePairs);
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName!),
                new(ClaimTypes.Email, user.Email!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            var accessToken = _tokenService.GenerateAccessToken(claims);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(14);
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                success_notification = "Welcome Back!!",
                AccessToken = accessToken,
                AccesstokenExpireIn = "3 min",
                RefreshToken = refreshToken
            });
        }

        [HttpPost("ResendEmailConfirmation")]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationRequest resendEmailConfirmationRequest)
        {
            var user = await _userManager.FindByEmailAsync(resendEmailConfirmationRequest.EmailOrUserName) ?? await _userManager.FindByNameAsync(resendEmailConfirmationRequest.EmailOrUserName);

            if (user is null)
                return NotFound(new
                {
                    error_notification = "Invalid User Name / Email"
                });

            if (!user.EmailConfirmed)
            {
                // Send Email Confirmation
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var link = Url.Action(nameof(Confirm), "Account", new { area = "Identity", token = token, userId = user.Id }, Request.Scheme);

                await _emailSender.SendEmailAsync(user.Email!,
                    "Resend: Ecommerce522 - Confirm Your Email",
                    $"<h1>Please Confirm Your Email By Clicking <a href='{link}'>Here</a>.</h1>"
                    );
            }

            //return Ok(new
            //{
            //    success_notification = "Send Email Successfully, Please Confirm Your Account if Exist"
            //});

            return Created($"{Request.Scheme}://{Request.Host}/Identity/Account/Login", new
            {
                success_notification = "Send Email Successfully, Please Confirm Your Account if Exist"
            });
        }

        [HttpPost("ForgetPassword")]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordRequest forgetPasswordRequest)
        {
            var user = await _userManager.FindByEmailAsync(forgetPasswordRequest.EmailOrUserName) ?? await _userManager.FindByNameAsync(forgetPasswordRequest.EmailOrUserName);

            if (user is null)
                return NotFound(new ErrorModel
                {
                    Code = "Invalid User Name / Email",
                    Message = "Invalid User Name / Email",
                });

            var otp = RandomNumberGenerator.GetInt32(1000, 10000);

            var userOTPs = await _applicationUserOTPRepository.GetAsync(
                e => e.ApplicationUserId == user.Id && e.CreateAt > DateTime.UtcNow.AddHours(-24));

            if (userOTPs.ToList().Count > 5)
                return BadRequest(new ErrorModel
                {
                    Code = "Too Many Attempts Please Try Again Later",
                    Message = "Too Many Attempts Please Try Again Later",
                });

            await _applicationUserOTPRepository.CreateAsync(new()
            {
                OTP = otp.ToString(),
                ApplicationUserId = user.Id,
            });

            await _applicationUserOTPRepository.CommitAsync();

            await _emailSender.SendEmailAsync(user.Email!,
                "Ecommerce522 - Reset Password",
                $"<h1>Please Reset Password Using OTP: {otp}. Don't share it.</h1>"
                );

            return CreatedAtAction(nameof(ValidateOTP), new
            {
                userId = user.Id
            }, new SuccessModel
            {
                Message = "Send Email Successfully, Please Check"
            });
        }

        [HttpPost("ValidateOTP")]
        public async Task<IActionResult> ValidateOTP(ValidateOTPRequest validateOTPRequest)
        {
            var user = await _userManager.FindByIdAsync(validateOTPRequest.UserId);

            if (user is null) return NotFound();

            var validOTPs = await _applicationUserOTPRepository.GetAsync(e => e.ApplicationUserId == validateOTPRequest.UserId && e.isValid && e.ValidTo > DateTime.UtcNow);

            var matchedOtp = validOTPs.FirstOrDefault(e => e.OTP == validateOTPRequest.OTP);

            if (matchedOtp is null)
                return BadRequest(new ErrorModel
                {
                    Code = "Invalid OR Expired OTP",
                    Message = "Invalid OR Expired OTP",
                });

            matchedOtp.isValid = false;
            await _applicationUserOTPRepository.CommitAsync();

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            return Ok(new
            {
                Message = "Valid OTP",
                UserId = user.Id,
                ResetToken = resetToken
            });
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest resetPasswordRequest)
        {
            var user = await _userManager.FindByIdAsync(resetPasswordRequest.UserId);

            if (user is null)
                return NotFound();

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordRequest.Token, resetPasswordRequest.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);  

            return Created($"{Request.Scheme}://{Request.Host}/Identity/Account/Login", new
            {
                success_notification = "Change Password Successfully"
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenRequest tokenRequest)
        {
            string accessToken = tokenRequest.ExpiredAccessToken;
            string refreshToken = tokenRequest.RefreshToken;

            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);

            var userName = principal.Identity?.Name;

            if (userName is null) return NotFound();

            var user = await _userManager.FindByNameAsync(userName);

            if(user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return BadRequest(new ErrorModel()
                {
                    Code = "Invalid client request",
                    Message = "Invalid Access / Refresh Token"
                });
            }

            var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(14);

            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                AccessToken = newAccessToken,
                AccesstokenExpireIn = "3 min",
                RefreshToken = newRefreshToken
            });

        }
    }
}
