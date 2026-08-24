using EyewearsProject.Models;
using EyewearsProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EyewearsProject.Services.Email;

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