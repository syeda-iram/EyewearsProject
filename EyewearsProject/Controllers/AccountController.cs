using EyewearsProject.Models;
using EyewearsProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EyewearsProject.Services.Email;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace EyewearsProject.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        // =========================
        // REGISTER
        // =========================

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model,
            string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,

                // User must verify email before login
                EmailConfirmed = false,

                IsActive = true
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            // Generate Identity email confirmation token
            var token =
                await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Generate confirmation URL
            var confirmationUrl = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new
                {
                    userId = user.Id,
                    token = token
                },
                Request.Scheme);

            // Send verification email
            await _emailService.SendEmailAsync(
                user.Email!,
                "Verify your EyeCraft account",
                $"""
                <h2>Welcome to EyeCraft!</h2>

                <p>Hello {user.FullName},</p>

                <p>
                    Thank you for creating your EyeCraft account.
                </p>

                <p>
                    Please verify your email address by clicking
                    the button below:
                </p>

                <p>
                    <a href="{confirmationUrl}"
                       style="
                           display:inline-block;
                           padding:10px 20px;
                           background:#212529;
                           color:white;
                           text-decoration:none;
                           border-radius:5px;
                       ">
                        Verify My Email
                    </a>
                </p>

                <p>
                    If you did not create this account,
                    you can safely ignore this email.
                </p>

                <p>
                    Regards,<br />
                    EyeCraft Team
                </p>
                """);

            // DO NOT automatically sign the user in.
            // They must verify their email first.
            TempData["RegisteredEmail"] = user.Email;

            return RedirectToAction(
                nameof(RegisterConfirmation));
        }

        // =========================
        // REGISTRATION CONFIRMATION
        // =========================

        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // =========================
        // CONFIRM EMAIL
        // =========================

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(
            string userId,
            string token)
        {
            if (string.IsNullOrEmpty(userId) ||
                string.IsNullOrEmpty(token))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Confirm email using Identity
            var result =
                await _userManager.ConfirmEmailAsync(
                    user,
                    token);

            if (result.Succeeded)
            {
                return View("EmailConfirmed");
            }

            return View("EmailConfirmationFailed");
        }

        // =========================
        // LOGIN
        // =========================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user =
                await _userManager.FindByEmailAsync(model.Email);

            if (user == null ||
                !user.IsActive ||
                user.IsDeleted)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid login attempt.");

                return View(model);
            }

            // Explicitly check email verification
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please verify your email address before logging in.");

                return View(model);
            }

            var result =
                await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            ModelState.AddModelError(
                string.Empty,
                result.IsLockedOut
                    ? "Account locked. Try again later."
                    : "Invalid login attempt.");

            return View(model);
        }

        // =========================
        // FORGOT PASSWORD
        // =========================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Do not reveal whether the email exists
            if (user == null || user.IsDeleted || !user.IsActive)
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // Generate Identity password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Encode token so it is safe to put inside a URL
            var encodedToken = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(token));

            var resetUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new
                {
                    userId = user.Id,
                    token = encodedToken
                },
                Request.Scheme);

            var emailBody = $"""
        <h2>Reset Your EyeCraft Password</h2>

        <p>Hello {user.FullName},</p>

        <p>
            We received a request to reset your EyeCraft account password.
        </p>

        <p>
            <a href="{resetUrl}"
               style="
                   display:inline-block;
                   padding:10px 20px;
                   background:#212529;
                   color:white;
                   text-decoration:none;
                   border-radius:5px;
               ">
                Reset Your Password
            </a>
        </p>

        <p>
            If you did not request this, you can safely ignore this email.
        </p>

        <p>
            Regards,<br/>
            EyeCraft Team
        </p>
        """;

            await _emailService.SendEmailAsync(
                user.Email!,
                "Reset Your EyeCraft Password",
                emailBody);

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // =========================
        // RESET PASSWORD
        // =========================

        [HttpGet]
        public IActionResult ResetPassword(string? userId, string? token)
        {
            if (string.IsNullOrEmpty(userId) ||
                string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(ResetPasswordFailed));
            }

            return View(new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null ||
                user.IsDeleted ||
                !user.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid password reset request.");

                return View(model);
            }

            string decodedToken;

            try
            {
                // Decode the URL-safe token back to the original Identity token
                decodedToken = Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(model.Token));
            }
            catch
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid or corrupted password reset link.");

                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(
                user,
                decodedToken,
                model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Reset failed: {error.Code} - {error.Description}");
                }

                return View(model);
            }

            // Password reset succeeded.
            // Now send confirmation email.
            try
            {
                var confirmationEmailBody = $"""
            <h2>Password Reset Successfully</h2>

            <p>Hello {user.FullName},</p>

            <p>
                Your EyeCraft account password has been successfully reset.
            </p>

            <p>
                You can now log in using your new password.
            </p>

            <p>
                If you did not make this change, please contact EyeCraft support immediately.
            </p>

            <p>
                Regards,<br/>
                EyeCraft Team
            </p>
            """;

                await _emailService.SendEmailAsync(
                    user.Email!,
                    "EyeCraft Password Reset Successful",
                    confirmationEmailBody);
            }
            catch
            {
                // The password was already successfully changed.
                // Email failure should not undo the password reset.
            }

            return RedirectToAction(
                nameof(ResetPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPasswordFailed()
        {
            return View();
        }

        // =========================
        // LOGOUT
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Index",
                "Home");
        }
    }
}