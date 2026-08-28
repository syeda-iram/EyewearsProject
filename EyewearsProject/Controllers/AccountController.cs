using EyewearsProject.Models;
using EyewearsProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EyewearsProject.Services.Email;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using EyewearsProject.Services.Sms;

namespace EyewearsProject.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService, ISmsService smsService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _smsService = smsService;
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

        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // =========================
        // CONFIRM EMAIL
        // =========================

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
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
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !user.IsActive || user.IsDeleted)
            {
                ModelState.AddModelError(string.Empty,
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

            var result = await _signInManager.CheckPasswordSignInAsync(
                    user, model.Password, lockoutOnFailure: true);

            if(result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account is locked out. Please try again later.");
                return View(model);
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Login is not allowed for this account.");

                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The email or password is incorrect.");

                return View(model);
            }

            // ==========================================
            // TWO FACTOR AUTHENTICATION
            // ==========================================

            if (user.TwoFactorEnabled)
            {
                HttpContext.Session.SetString(
                    "TwoFactorUserId",
                    user.Id);

                if (!string.IsNullOrWhiteSpace(returnUrl))
                {
                    HttpContext.Session.SetString(
                        "TwoFactorReturnUrl",
                        returnUrl);
                }

                HttpContext.Session.SetString(
                    "TwoFactorRememberMe",
                    model.RememberMe.ToString());

                return RedirectToAction(
                    nameof(ChooseTwoFactorMethod));
            }

            // Normal login
            await _signInManager.SignInAsync(
                user,
                model.RememberMe);

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(
                "Index",
                "Home");

        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTwoFactor(TwoFactorAuthenticationViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            if (!model.TwoFactorEnabled)
            {
                user.TwoFactorEnabled = false;
                user.TwoFactorMethod = null;

                await _userManager.UpdateAsync(user);

                TempData["Success"] =
                    "Two-factor authentication has been disabled.";

                return RedirectToAction(nameof(TwoFactorAuthentication));
            }

            if (string.IsNullOrWhiteSpace(model.SelectedMethod))
            {
                TempData["Error"] =
                    "Please select a verification method.";

                return RedirectToAction(nameof(TwoFactorAuthentication));
            }

            if (model.SelectedMethod == "Email")
            {
                if (string.IsNullOrWhiteSpace(user.Email) ||
                    !user.EmailConfirmed)
                {
                    TempData["Error"] =
                        "Your email address must be verified before using email authentication.";

                    return RedirectToAction(nameof(TwoFactorAuthentication));
                }
            }

            if (model.SelectedMethod == "SMS")
            {
                if (string.IsNullOrWhiteSpace(user.PhoneNumber) ||
                    !user.PhoneVerified)
                {
                    TempData["Error"] =
                        "Your phone number must be verified before using SMS authentication.";

                    return RedirectToAction(nameof(TwoFactorAuthentication));
                }
            }

            user.TwoFactorEnabled = true;
            user.TwoFactorMethod = model.SelectedMethod;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            TempData["Success"] =
                $"Two-factor authentication has been enabled using {model.SelectedMethod}.";

            return RedirectToAction(nameof(TwoFactorAuthentication));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ChooseTwoFactorMethod()
        {
            var userId =
                HttpContext.Session.GetString("TwoFactorUserId");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction(nameof(Login));

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null ||
                !user.IsActive ||
                user.IsDeleted ||
                !user.TwoFactorEnabled)
            {
                return RedirectToAction(nameof(Login));
            }

            var emailAvailable =
                !string.IsNullOrWhiteSpace(user.Email) &&
                user.EmailConfirmed;

            var phoneAvailable =
                !string.IsNullOrWhiteSpace(user.PhoneNumber) &&
                user.PhoneVerified;

            if (!emailAvailable && !phoneAvailable)
            {
                await _signInManager.SignOutAsync();

                HttpContext.Session.Remove("TwoFactorUserId");

                return RedirectToAction(nameof(Login));
            }

            // If user has selected a preferred method,
            // still allow them to choose another available method.
            return View(new TwoFactorAuthenticationViewModel
            {
                TwoFactorEnabled = true,
                EmailAvailable = emailAvailable,
                PhoneAvailable = phoneAvailable,
                CurrentMethod = user.TwoFactorMethod
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTwoFactorCode(string method)
        {
            var userId =
                HttpContext.Session.GetString("TwoFactorUserId");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction(nameof(Login));

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null ||
                !user.IsActive ||
                user.IsDeleted ||
                !user.TwoFactorEnabled)
            {
                return RedirectToAction(nameof(Login));
            }

            if (method == "Email")
            {
                if (string.IsNullOrWhiteSpace(user.Email) ||
                    !user.EmailConfirmed)
                {
                    TempData["Error"] =
                        "Email authentication is not available.";

                    return RedirectToAction(
                        nameof(ChooseTwoFactorMethod));
                }

                var code =
                    await _userManager.GenerateTwoFactorTokenAsync(
                        user,
                        TokenOptions.DefaultEmailProvider);

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Your EyeCraft verification code",
                    $"""
            <h2>EyeCraft Verification Code</h2>

            <p>Hello {user.FullName},</p>

            <p>
                Your two-factor authentication code is:
            </p>

            <h1 style="letter-spacing:6px;">
                {code}
            </h1>

            <p>
                This code will expire in 5 minutes.
            </p>

            <p>
                If you did not try to log in, please secure your account.
            </p>

            <p>
                Regards,<br/>
                EyeCraft Team
            </p>
            """);
            }
            else if (method == "SMS")
            {
                if (string.IsNullOrWhiteSpace(user.PhoneNumber) ||
                    !user.PhoneVerified)
                {
                    TempData["Error"] =
                        "SMS authentication is not available.";

                    return RedirectToAction(
                        nameof(ChooseTwoFactorMethod));
                }

                var code =
                    await _userManager.GenerateTwoFactorTokenAsync(
                        user,
                        TokenOptions.DefaultPhoneProvider);

                await _smsService.SendSmsAsync(
                    user.PhoneNumber,
                    $"Your EyeCraft verification code is {code}. It expires in 5 minutes.");
            }
            else
            {
                TempData["Error"] =
                    "Invalid verification method.";

                return RedirectToAction(
                    nameof(ChooseTwoFactorMethod));
            }

            HttpContext.Session.SetString(
                "TwoFactorMethod",
                method);

            HttpContext.Session.SetString(
                "TwoFactorCodeSentAt",
                DateTime.UtcNow.ToString("O"));

            return RedirectToAction(nameof(VerifyTwoFactor));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerifyTwoFactor()
        {
            var userId = HttpContext.Session.GetString("TwoFactorUserId");
            var method = HttpContext.Session.GetString("TwoFactorMethod");
            var sentAtString = HttpContext.Session.GetString("TwoFactorCodeSentAt");

            if (string.IsNullOrEmpty(userId) ||
                string.IsNullOrEmpty(method) ||
                string.IsNullOrEmpty(sentAtString))
            {
                return RedirectToAction(nameof(Login));
            }

            if (!DateTime.TryParse(
                sentAtString,
                out var sentAt))
            {
                return RedirectToAction(nameof(Login));
            }

            var elapsed = DateTime.UtcNow - sentAt;
            var remaining = Math.Max(0, 300 - (int)elapsed.TotalSeconds);

            if (remaining <= 0)
            {
                TempData["Error"] = "Your verification code has expired.";
                return RedirectToAction(nameof(ChooseTwoFactorMethod));
            }

            return View(new VerifyTwoFactorOtpViewModel
                {
                    Method = method,
                    RemainingSeconds = remaining
                });
        }
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTwoFactor(
            VerifyTwoFactorOtpViewModel model)
        {
            var userId =
                HttpContext.Session.GetString("TwoFactorUserId");

            var method =
                HttpContext.Session.GetString("TwoFactorMethod");

            var sentAtString =
                HttpContext.Session.GetString("TwoFactorCodeSentAt");


            // =========================
            // CHECK 2FA SESSION
            // =========================

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(method) ||
                string.IsNullOrWhiteSpace(sentAtString))
            {
                return RedirectToAction(nameof(Login));
            }


            // =========================
            // FIND USER
            // =========================

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null ||
                !user.IsActive ||
                user.IsDeleted)
            {
                return RedirectToAction(nameof(Login));
            }


            // =========================
            // PARSE SENT TIME
            // =========================

            if (!DateTime.TryParse(
                    sentAtString,
                    out var sentAt))
            {
                ClearTwoFactorLoginSession();

                return RedirectToAction(nameof(Login));
            }


            // =========================
            // CHECK 5 MINUTE EXPIRATION
            // =========================

            var elapsedSeconds =
                (int)(DateTime.UtcNow - sentAt).TotalSeconds;

            var remainingSeconds =
                Math.Max(0, 300 - elapsedSeconds);

            model.RemainingSeconds =
                Math.Min(300, remainingSeconds);

            model.Method = method;


            if (remainingSeconds <= 0)
            {
                ClearTwoFactorLoginSession();

                ModelState.AddModelError(
                    string.Empty,
                    "The verification code has expired. Please request a new code.");

                return View(model);
            }


            // =========================
            // VALIDATE METHOD
            // =========================

            string provider;

            if (method.Equals(
                    "Email",
                    StringComparison.OrdinalIgnoreCase))
            {
                provider =
                    TokenOptions.DefaultEmailProvider;
            }
            else if (method.Equals(
                         "SMS",
                         StringComparison.OrdinalIgnoreCase))
            {
                provider =
                    TokenOptions.DefaultPhoneProvider;
            }
            else
            {
                ClearTwoFactorLoginSession();

                return RedirectToAction(nameof(Login));
            }


            // =========================
            // VALIDATE OTP
            // =========================

            if (string.IsNullOrWhiteSpace(model.Otp))
            {
                ModelState.AddModelError(
                    nameof(model.Otp),
                    "Please enter the verification code.");

                return View(model);
            }


            var valid =
                await _userManager.VerifyTwoFactorTokenAsync(
                    user,
                    provider,
                    model.Otp.Trim());


            if (!valid)
            {
                ModelState.AddModelError(
                    nameof(model.Otp),
                    "Invalid verification code.");

                return View(model);
            }


            // =========================
            // REMEMBER ME
            // =========================

            var rememberMeString =
                HttpContext.Session.GetString(
                    "TwoFactorRememberMe");

            bool.TryParse(
                rememberMeString,
                out var rememberMe);


            // =========================
            // RETURN URL
            // =========================

            var returnUrl =
                HttpContext.Session.GetString(
                    "TwoFactorReturnUrl");


            // =========================
            // COMPLETE LOGIN
            // =========================

            await _signInManager.SignInAsync(
                user,
                rememberMe);


            // =========================
            // CLEAR 2FA SESSION
            // =========================

            ClearTwoFactorLoginSession();


            // =========================
            // REDIRECT
            // =========================

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(
                "Index",
                "Home");
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
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
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
        // PROFILE
        // =========================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            var model = new ProfileViewModel
            {
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                ProfileImage = user.ProfileImage
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.PhoneNumber = model.Phone;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;
            user.ProfileImage = model.ProfileImage;

            user.FullName = $"{model.FirstName} {model.LastName}".Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            TempData["Success"] = "Profile updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        // =========================
        // SETTINGS
        // =========================

        [Authorize]
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            return View(user);
        }


        // =========================
        // TWO-FACTOR AUTHENTICATION
        // =========================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            if (user.IsDeleted || !user.IsActive)
                return RedirectToAction("Index", "Home");

            var model = new TwoFactorAuthenticationViewModel
            {
                TwoFactorEnabled = user.TwoFactorEnabled,

                EmailAvailable = !string.IsNullOrWhiteSpace(user.Email) && user.EmailConfirmed,
                PhoneAvailable = !string.IsNullOrWhiteSpace(user.PhoneNumber) && user.PhoneVerified,
                MaskedEmail = MaskEmail(user.Email),
                MaskedPhone = MaskPhone(user.PhoneNumber),

                CurrentMethod = string.IsNullOrWhiteSpace(user.TwoFactorMethod) ? "Email" : user.TwoFactorMethod,
                SelectedMethod = string.IsNullOrWhiteSpace(user.TwoFactorMethod) ? "Email" : user.TwoFactorMethod
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTwoFactorOtp(string method)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            if (user.IsDeleted || !user.IsActive)
                return RedirectToAction("Index", "Home");

            method = method?.Trim() ?? "";

            if (!method.Equals("Email", StringComparison.OrdinalIgnoreCase) &&
                !method.Equals("SMS", StringComparison.OrdinalIgnoreCase))
            {
                TempData["TwoFactorError"] =
                    "Please select a valid authentication method.";

                return RedirectToAction(nameof(TwoFactorAuthentication));
            }

            // -------------------------
            // EMAIL OTP
            // -------------------------

            if (method.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    TempData["TwoFactorError"] =
                        "No email address is associated with your account.";

                    return RedirectToAction(nameof(TwoFactorAuthentication));
                }

                var otp = Random.Shared.Next(100000, 1000000).ToString();

                HttpContext.Session.SetString(
                    "TwoFactorOtp",
                    otp);

                HttpContext.Session.SetString(
                    "TwoFactorMethod",
                    "Email");

                HttpContext.Session.SetString(
                    "TwoFactorOtpExpires",
                    DateTimeOffset.UtcNow
                        .AddMinutes(5)
                        .ToUnixTimeSeconds()
                        .ToString());

                var emailBody = $"""
                <h2>EyeCraft Two-Factor Authentication</h2>

                <p>Hello {user.FullName},</p>

                <p>
                    Your one-time verification code is:
                </p>

                <div style="
                    font-size:30px;
                    font-weight:bold;
                    letter-spacing:8px;
                    margin:20px 0;">
                    {otp}
                </div>

                <p>
                    This code will expire in <strong>5 minutes</strong>.
                </p>

                <p>
                    If you did not request this code, please ignore this email.
                </p>

                <p>
                    Regards,<br/>
                    EyeCraft Team
                </p>
                """;

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Your EyeCraft Verification Code",
                    emailBody);

                return RedirectToAction(
                    nameof(VerifyTwoFactorOtp),
                    new { method = "Email" });
            }

            // -------------------------
            // SMS OTP
            // -------------------------
            //
            // SMS provider is not integrated yet.
            // We deliberately do not pretend to send an SMS.
            //

            TempData["TwoFactorError"] =
                "SMS authentication is not available yet. Please use Email for now.";

            return RedirectToAction(nameof(TwoFactorAuthentication));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> VerifyTwoFactorOtp(string method)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            var savedMethod =
                HttpContext.Session.GetString("TwoFactorMethod");

            var expiry =
                HttpContext.Session.GetString("TwoFactorOtpExpires");

            if (string.IsNullOrWhiteSpace(savedMethod) ||
                string.IsNullOrWhiteSpace(expiry))
            {
                TempData["TwoFactorError"] =
                    "Your verification session has expired. Please request a new OTP.";

                return RedirectToAction(nameof(TwoFactorAuthentication));
            }

            if (!savedMethod.Equals(
                    method,
                    StringComparison.OrdinalIgnoreCase))
            {
                method = savedMethod;
            }

            if (!long.TryParse(expiry, out var expiryTimestamp))
            {
                ClearTwoFactorSession();

                TempData["TwoFactorError"] =
                    "Invalid verification session.";

                return RedirectToAction(nameof(TwoFactorAuthentication));
            }

            var remaining =
                (int)Math.Max(
                    0,
                    expiryTimestamp -
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            if (remaining <= 0)
            {
                ClearTwoFactorSession();

                TempData["TwoFactorError"] =
                    "Your OTP has expired. Please request a new one.";

                return RedirectToAction(nameof(TwoFactorAuthentication));
            }

            var model = new VerifyTwoFactorOtpViewModel
            {
                Method = savedMethod,
                RemainingSeconds = remaining
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTwoFactorOtp(
            VerifyTwoFactorOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            var savedOtp =
                HttpContext.Session.GetString("TwoFactorOtp");

            var savedMethod =
                HttpContext.Session.GetString("TwoFactorMethod");

            var expiryString =
                HttpContext.Session.GetString("TwoFactorOtpExpires");

            if (string.IsNullOrWhiteSpace(savedOtp) ||
                string.IsNullOrWhiteSpace(savedMethod) ||
                string.IsNullOrWhiteSpace(expiryString))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your OTP session has expired. Please request a new OTP.");

                return View(model);
            }

            if (!long.TryParse(
                    expiryString,
                    out var expiryTimestamp))
            {
                ClearTwoFactorSession();

                ModelState.AddModelError(
                    string.Empty,
                    "Invalid OTP session.");

                return View(model);
            }

            var remaining =
                (int)Math.Max(
                    0,
                    expiryTimestamp -
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            model.RemainingSeconds = remaining;

            if (remaining <= 0)
            {
                ClearTwoFactorSession();

                ModelState.AddModelError(
                    string.Empty,
                    "Your OTP has expired. Please request a new OTP.");

                return View(model);
            }

            if (!savedMethod.Equals(
                    model.Method,
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid authentication method.");

                return View(model);
            }

            if (!string.Equals(
                    savedOtp,
                    model.Otp.Trim(),
                    StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    nameof(model.Otp),
                    "The OTP you entered is incorrect.");

                return View(model);
            }

            // OTP verified successfully
            user.TwoFactorEnabled = true;

            await _userManager.UpdateAsync(user);

            ClearTwoFactorSession();

            TempData["TwoFactorSuccess"] =
                "Two-factor authentication has been enabled successfully.";

            return RedirectToAction(
                nameof(TwoFactorAuthentication));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            user.TwoFactorEnabled = false;

            await _userManager.UpdateAsync(user);

            ClearTwoFactorSession();

            TempData["TwoFactorSuccess"] =
                "Two-factor authentication has been disabled.";

            return RedirectToAction(
                nameof(TwoFactorAuthentication));
        }

        private void ClearTwoFactorLoginSession()
        {
            HttpContext.Session.Remove("TwoFactorUserId");
            HttpContext.Session.Remove("TwoFactorMethod");
            HttpContext.Session.Remove("TwoFactorCodeSentAt");
            HttpContext.Session.Remove("TwoFactorRememberMe");
            HttpContext.Session.Remove("TwoFactorReturnUrl");
        }

        private void ClearTwoFactorSession()
        {
            HttpContext.Session.Remove("TwoFactorOtp");
            HttpContext.Session.Remove("TwoFactorMethod");
            HttpContext.Session.Remove("TwoFactorOtpExpires");
        }

        private static string? MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var parts = email.Split('@');

            if (parts.Length != 2)
                return email;

            var name = parts[0];

            if (name.Length <= 2)
                return $"{name[0]}***@{parts[1]}";

            return $"{name[0]}***{name[^1]}@{parts[1]}";
        }

        private static string? MaskPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            if (phone.Length <= 4)
                return "****";

            return new string(
                       '*',
                       Math.Max(0, phone.Length - 4))
                   + phone[^4..];
        }


        // =========================
        // DELETE ACCOUNT
        // =========================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            if (user.IsDeleted)
                return RedirectToAction("Index", "Home");

            return View(new DeleteAccountViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(
            DeleteAccountViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            if (user.IsDeleted)
                return RedirectToAction("Index", "Home");

            // Verify current password
            var passwordValid = await _userManager.CheckPasswordAsync(
                user,
                model.Password);

            if (!passwordValid)
            {
                ModelState.AddModelError(
                    nameof(model.Password),
                    "The password you entered is incorrect.");

                return View(model);
            }

            // Soft delete account
            user.IsDeleted = true;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

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

            // Send account deletion confirmation email
            try
            {
                var emailBody = $"""
                <h2>Your EyeCraft Account Has Been Deleted</h2>

                <p>Hello {user.FullName},</p>

                <p>
                    Your EyeCraft account has been successfully deleted
                    as requested.
                </p>

                <p>
                    We're sorry to see you go.
                </p>

                <p>
                    Thank you for being a part of EyeCraft.
                </p>

                <p>
                    If you did not request this account deletion,
                    please contact EyeCraft support immediately.
                </p>

                <p>
                    Regards,<br/>
                    EyeCraft Team
                </p>
                """;

                await _emailService.SendEmailAsync(
                    user.Email!,
                    "Your EyeCraft Account Has Been Deleted",
                    emailBody);
            }
            catch
            {
                // Account deletion has already succeeded.
                // Email failure should not undo the deletion.
            }

            // Sign the user out
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                nameof(AccountDeleted));
        }

        // =========================
        // ACCOUNT DELETED
        // =========================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccountDeleted()
        {
            return View();
        }


        // =========================
        // CHANGE PASSWORD
        // =========================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            if (user.IsDeleted || !user.IsActive)
                return RedirectToAction("Index", "Home");

            return View(new ChangePasswordViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            if (user.IsDeleted || !user.IsActive)
                return RedirectToAction("Index", "Home");

            // Verify the current password first
            var currentPasswordValid =
                await _userManager.CheckPasswordAsync(
                    user,
                    model.CurrentPassword);

            if (!currentPasswordValid)
            {
                ModelState.AddModelError(
                    nameof(model.CurrentPassword),
                    "The current password you entered is incorrect.");

                return View(model);
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

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

            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            // Send confirmation email
            try
            {
                var emailBody = $"""
                <h2>Your EyeCraft Password Has Been Updated</h2>

                <p>Hello {user.FullName},</p>

                <p>
                    Your EyeCraft account password has been successfully
                    changed.
                </p>

                <p>
                    If you made this change, no further action is required.
                </p>

                <p>
                    If you did not change your password, please contact
                    EyeCraft support immediately.
                </p>

                <p>
                    Regards,<br/>
                    EyeCraft Team
                </p>
                """;

                await _emailService.SendEmailAsync(
                    user.Email!,
                    "EyeCraft Password Updated",
                    emailBody);
            }
            catch
            {
                // Password has already been changed.
                // Email failure must not undo the password change.
            }

            TempData["PasswordChanged"] =
                "Your password has been changed successfully.";

            return RedirectToAction(nameof(ChangePasswordConfirmation));
        }

        // ===============================
        // CHANGE PASSWORD CONFIRMATION
        // ===============================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangePasswordConfirmation()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(nameof(Login));

            if (user.IsDeleted || !user.IsActive)
                return RedirectToAction("Index", "Home");

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