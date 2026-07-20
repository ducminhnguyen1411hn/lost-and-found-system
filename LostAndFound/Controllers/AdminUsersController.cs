using LostAndFound.Models;
using LostAndFound.Models.ViewModels.Admin;
using LostAndFound.Services.Interfaces;
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
        private readonly IAuditService _audit;

        public AdminUsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IAuditService audit)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _audit = audit;
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
                    Roles = await _userManager.GetRolesAsync(user),
                    IsBlocked = user.IsBlocked,
                    IsPostingBlocked = user.IsPostingBlocked
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
                FullName = user.FullName ?? "N/A",
                IsBlocked = user.IsBlocked,
                IsPostingBlocked = user.IsPostingBlocked
            };

            var validRoles = new[] { "Member", "Staff", "Admin" };
            foreach(var role in allRoles.Where(r => validRoles.Contains(r.Name)))
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
        public async Task<IActionResult> Edit(EditUserRolesViewModel model, string selectedRole)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // CHỐT CHẶN BẢO MẬT 1: Admin không được tự gỡ quyền Admin của chính mình
            var currentLoggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (user.Id == currentLoggedInUserId && selectedRole != "Admin")
            {
                ModelState.AddModelError(string.Empty, "Không thể tự gỡ quyền Admin của chính bạn.");
                return View(model);
            }

            // CHỐT CHẶN BẢO MẬT 2: Không cho phép xóa vị Admin cuối cùng trong hệ thống
            if (selectedRole != "Admin")
            {
                var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                if (allAdmins.Count <= 1 && allAdmins.Any(u => u.Id == user.Id))
                {
                    ModelState.AddModelError(string.Empty, "Không thể gỡ Admin cuối cùng của hệ thống.");
                    return View(model);
                }
            }

            // CHỐT CHẶN BẢO MẬT 3: Admin không được tự chặn chính mình
            if (user.Id == currentLoggedInUserId && model.IsBlocked)
            {
                ModelState.AddModelError(string.Empty, "Không thể tự khoá tài khoản của bạn.");
                return View(model);
            }

            // Đảm bảo chỉ có 1 role được chọn
            if (string.IsNullOrEmpty(selectedRole))
            {
                ModelState.AddModelError(string.Empty, "Mỗi người dùng phải có ít nhất một role.");
                return View(model);
            }

            // CHỐT CHẶN BẢO MẬT 4: Chỉ cho phép gán 3 roles hợp lệ
            var validRoles = new[] { "Member", "Staff", "Admin" };
            if (!validRoles.Contains(selectedRole))
            {
                ModelState.AddModelError(string.Empty, "Vai trò không hợp lệ. Chỉ cho phép Member, Staff, hoặc Admin.");
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            // Snapshot BEFORE mutating, so we can audit exactly what changed.
            var oldRole = currentRoles.FirstOrDefault();
            var wasBlocked = user.IsBlocked;
            var wasPostingBlocked = user.IsPostingBlocked;

            // Xóa tất cả roles hiện tại và chỉ gán role được chọn (đảm bảo 1 role duy nhất)
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, selectedRole);

            // Cập nhật blocking flags
            user.IsBlocked = model.IsBlocked;
            user.IsPostingBlocked = model.IsPostingBlocked;
            await _userManager.UpdateAsync(user);

            // FR-LOG: these role/block changes are the most sensitive admin actions; log each real change.
            var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (oldRole != selectedRole)
                await _audit.LogAsync(actorId, "RoleChanged", "User", user.Id,
                    oldRole, selectedRole, $"Đổi vai trò của {user.Email}: {oldRole ?? "(chưa có)"} → {selectedRole}", isPublic: false);
            if (wasBlocked != model.IsBlocked)
                await _audit.LogAsync(actorId, model.IsBlocked ? "UserBlocked" : "UserUnblocked", "User", user.Id,
                    null, null, $"{(model.IsBlocked ? "Khoá" : "Mở khoá")} tài khoản {user.Email}", isPublic: false);
            if (wasPostingBlocked != model.IsPostingBlocked)
                await _audit.LogAsync(actorId, model.IsPostingBlocked ? "PostingBlocked" : "PostingUnblocked", "User", user.Id,
                    null, null, $"{(model.IsPostingBlocked ? "Chặn" : "Bỏ chặn")} đăng bài của {user.Email}", isPublic: false);

            TempData["SuccessMessage"] = $"Cập nhật thành công cho {user.Email}.";
            return RedirectToAction(nameof(Index));
        }
    }
}