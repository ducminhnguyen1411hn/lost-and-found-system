using LostAndFound.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LostAndFound.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Ghi nhớ đăng nhập")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {

                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null && existingUser.IsBlocked)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                    return Page();
                }

                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    if (existingUser != null && await _userManager.IsInRoleAsync(existingUser, "Admin"))
                        return RedirectToAction("Index", "Admin");
                    return LocalRedirect(returnUrl);
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            return Page();
        }

        public IActionResult OnPostExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            properties.SetParameter("prompt", "select_account");

            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!string.IsNullOrEmpty(remoteError))
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return Page();
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogWarning("External login information is null.");
                return RedirectToPage("./Login");
            }

            var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email)
                ?? throw new InvalidOperationException("Email claim not found.");

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in via external provider.");

                var user = await _userManager.FindByEmailAsync(email);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return LocalRedirect("/Admin");
                }

                return LocalRedirect(returnUrl);
            }

            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Name) ?? email
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                _logger.LogError($"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                ModelState.AddModelError(string.Empty, "Failed to create account.");
                return Page();
            }

            var loginResult = await _userManager.AddLoginAsync(newUser, info);
            if (!loginResult.Succeeded)
            {
                _logger.LogError($"Failed to add login: {string.Join(", ", loginResult.Errors.Select(e => e.Description))}");
                ModelState.AddModelError(string.Empty, "Failed to add login provider.");
                return Page();
            }

            await _userManager.AddToRoleAsync(newUser, "Member");

            await _signInManager.SignInAsync(newUser, isPersistent: false);
            _logger.LogInformation("User created and logged in via external provider.");
            return LocalRedirect(returnUrl);
        }
    }
}