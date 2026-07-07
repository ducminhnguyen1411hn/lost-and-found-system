using System.ComponentModel.DataAnnotations;
using LostAndFound.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LostAndFound.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty] public InputModel Input { get; set; }
        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required][EmailAddress] public string Email { get; set; }
            [Required][StringLength(100, MinimumLength = 6)][DataType(DataType.Password)] public string Password { get; set; }
            [DataType(DataType.Password)][Compare("Password")] public string ConfirmPassword { get; set; }
            [Required] public string FullName { get; set; }
            [Required] public string StudentOrStaffCode { get; set; }
            public string Department { get; set; }
        }

        public void OnGet(string returnUrl = null) => ReturnUrl = returnUrl;

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FullName = Input.FullName,
                    StudentOrStaffCode = Input.StudentOrStaffCode,
                    Department = Input.Department,
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member"); // FR-AUTH-01
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}