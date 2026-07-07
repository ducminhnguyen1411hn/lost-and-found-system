using LostAndFound.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LostAndFound.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /AdminUsers
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                model.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName ?? "N/A",
                    Email = user.Email,
                    Code = user.StudentOrStaffCode ?? "N/A",
                    Department = user.Department ?? "N/A",
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }
            return View(model);
        }

        // GET: /AdminUsers/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            var model = new EditUserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName ?? "N/A"
            };

            // Tự động sinh danh sách checkbox từ các Role có trong DB
            foreach (var role in allRoles)
            {
                model.Roles.Add(new RoleSelectionViewModel
                {
                    RoleName = role.Name,
                    IsSelected = userRoles.Contains(role.Name)
                });
            }

            return View(model);
        }

        // POST: /AdminUsers/Edit
        [HttpPost]
        [ValidateAntiForgeryToken] // Chống tấn công CSRF bảo mật cho Admin
        public async Task<IActionResult> Edit(EditUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // CHỐT CHẶN BẢO MẬT 1: Admin không được tự gỡ quyền Admin của chính mình
            var currentLoggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdminSelected = model.Roles.Any(r => r.RoleName == "Admin" && r.IsSelected);

            if (user.Id == currentLoggedInUserId && !isAdminSelected)
            {
                ModelState.AddModelError(string.Empty, "Guards: You cannot remove your own Admin role.");
                return View(model);
            }

            // CHỐT CHẶN BẢO MẬT 2: Không cho phép xóa vị Admin cuối cùng trong hệ thống
            if (!isAdminSelected)
            {
                var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                if (allAdmins.Count <= 1 && allAdmins.Any(u => u.Id == user.Id))
                {
                    ModelState.AddModelError(string.Empty, "Guards: Cannot remove the last remaining Admin in the system.");
                    return View(model);
                }
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Lọc ra các quyền cần thêm và các quyền cần xóa bỏ
            var rolesToAdd = model.Roles.Where(r => r.IsSelected && !currentRoles.Contains(r.RoleName)).Select(r => r.RoleName);
            var rolesToRemove = model.Roles.Where(r => !r.IsSelected && currentRoles.Contains(r.RoleName)).Select(r => r.RoleName);

            // Thực thi cập nhật Database hiệu năng cao (Bulk operation)
            if (rolesToAdd.Any()) await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (rolesToRemove.Any()) await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            TempData["SuccessMessage"] = $"Roles updated successfully for {user.Email}.";
            return RedirectToAction(nameof(Index));
        }
    }
}