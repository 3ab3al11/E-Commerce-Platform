using ECommerce522.ViewModels;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ECommerce522.Areas.Identity.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IRepository<ApplicationUserOTP> _applicationUserOTPRepository;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, IRepository<ApplicationUserOTP> applicationUserOTPRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _applicationUserOTPRepository = applicationUserOTPRepository;
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (!ModelState.IsValid)
                return View(registerVM);

            // Create User In DB
            ApplicationUser user = new()
            {
                Email = registerVM.Email,
                UserName = registerVM.UserName,
                Name = registerVM.Name,
                Address = registerVM.Address,
            };

            //ApplicationUser user = registerVM.Adapt<ApplicationUser>();

            var result = await _userManager.CreateAsync(user, registerVM.Password);

            if (!result.Succeeded)
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, item.Code);
                    return View(registerVM);
                }

            // Send Email Confirmation
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(Confirm), "Account", new { area = "Identity", token = token, userId = user.Id }, Request.Scheme);

            await _emailSender.SendEmailAsync(registerVM.Email,
                "Ecommerce522 - Confirm Your Email",
                $"<h1>Please Confirm Your Email By Clicking <a href='{link}'>Here</a>.</h1>"
                );

            // Save User in his role
            await _userManager.AddToRoleAsync(user, SD.CUSTOMER_ROLE);

            TempData["success-notification"] = "Add Account Successfully, Please Confirm Your Account";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Confirm(string token, string userId)
        {

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
                TempData["error-notification"] = String.Join(", ", result.Errors.Select(e => e.Code));
            else
                TempData["success-notification"] = "Confirm Account Successfully";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (!ModelState.IsValid)
                return View(loginVM);

            var user = await _userManager.FindByEmailAsync(loginVM.EmailOrUserName) ?? await _userManager.FindByNameAsync(loginVM.EmailOrUserName);

            if(user is null)
            {
                // Add msg in Model State
                ModelState.AddModelError(string.Empty, "Invalid User Name / Email OR Password");

                // Push Notification
                TempData["error-notification"] = "Invalid User Name / Email OR Password";

                return View(loginVM);
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, lockoutOnFailure: true, isPersistent: loginVM.RememberMe);

            if (!result.Succeeded)
            {
                if(!user.LockoutEnabled)
                    ModelState.AddModelError(string.Empty, $"Locked User till {user.LockoutEnd}");

                if (result.IsNotAllowed)
                    ModelState.AddModelError(string.Empty, "Invalid User Name / Email OR Password");
                else if(result.IsLockedOut)
                {
                    // Send Email
                    ModelState.AddModelError(string.Empty, "Too Many Attempts, Please Try Later");
                }

                return View(loginVM);
            }

            // Send Email
            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }

        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationVM resendEmailConfirmationVM)
        {
            if (!ModelState.IsValid)
                return View(resendEmailConfirmationVM);

            var user = await _userManager.FindByEmailAsync(resendEmailConfirmationVM.EmailOrUserName) ?? await _userManager.FindByNameAsync(resendEmailConfirmationVM.EmailOrUserName);

            if (user is null)
            {
                // Add msg in Model State
                ModelState.AddModelError(string.Empty, "Invalid User Name / Email");

                return View(resendEmailConfirmationVM);
            }

            if(!user.EmailConfirmed)
            {
                // Send Email Confirmation
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var link = Url.Action(nameof(Confirm), "Account", new { area = "Identity", token = token, userId = user.Id }, Request.Scheme);

                await _emailSender.SendEmailAsync(user.Email!,
                    "Resend: Ecommerce522 - Confirm Your Email",
                    $"<h1>Please Confirm Your Email By Clicking <a href='{link}'>Here</a>.</h1>"
                    );
            }

            // Save User in his role

            TempData["success-notification"] = "Send Email Successfully, Please Confirm Your Account if Exist";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM forgetPasswordVM)
        {
            if (!ModelState.IsValid)
                return View(forgetPasswordVM);

            var user = await _userManager.FindByEmailAsync(forgetPasswordVM.EmailOrUserName) ?? await _userManager.FindByNameAsync(forgetPasswordVM.EmailOrUserName);

            if (user is null)
            {
                // Add msg in Model State
                ModelState.AddModelError(string.Empty, "Invalid User Name / Email");

                return View(forgetPasswordVM);
            }

            var otp = RandomNumberGenerator.GetInt32(1000, 10000);

            var recentOtps = await _applicationUserOTPRepository.GetAsync(
                e => e.ApplicationUserId == user.Id && e.CreateAt > DateTime.UtcNow.AddHours(-24));
            if (recentOtps.Count() >= 5)
            {
                ModelState.AddModelError(string.Empty, "Too Many Attempts Please Try Again Later");
                return View(forgetPasswordVM);
            }

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

            // Save User in his role

            TempData["success-notification"] = "Send Email Successfully, Please Check";
            return RedirectToAction("ValidateOTP", new { userId = user.Id });
        }

        [HttpGet]
        public IActionResult ValidateOTP(string userId)
        {
            return View(new ValidateOTPVM()
            {
                UserId = userId
            });
        }

        [HttpPost]
        public async Task<IActionResult> ValidateOTP(ValidateOTPVM validateOTPVM)
        {
            if(!ModelState.IsValid)
                return View(validateOTPVM);

            var user = await _userManager.FindByIdAsync(validateOTPVM.UserId);

            if (user is null)
                return NotFound();

            var validOTPs = await _applicationUserOTPRepository.GetAsync(e => e.ApplicationUserId == validateOTPVM.UserId && e.isValid && e.ValidTo > DateTime.UtcNow);

            var matchedOtp = validOTPs.FirstOrDefault(e => e.OTP == validateOTPVM.OTP);

            if (matchedOtp is null)
            {
                // Add msg in Model State
                ModelState.AddModelError(string.Empty, "Invalid OR Expired OTP");

                return View(validateOTPVM);
            }

            matchedOtp.isValid = false;
            await _applicationUserOTPRepository.CommitAsync();

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            TempData["success-notification"] = "Valid OTP";
            return RedirectToAction("ResetPassword", new { userId = user.Id, token = resetToken });
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            return View(new ResetPasswordVM()
            {
                UserId = userId,
                Token = token
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM resetPasswordVM)
        {
            if (!ModelState.IsValid)
                return View(resetPasswordVM);

            var user = await _userManager.FindByIdAsync(resetPasswordVM.UserId);

            if (user is null)
                return NotFound();

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordVM.Token, resetPasswordVM.Password);

            if (!result.Succeeded)
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, item.Code);
                    return View(resetPasswordVM);
                }

            TempData["success-notification"] = "Change Password Successfully";
            return RedirectToAction("Login");
        }
    }
}
