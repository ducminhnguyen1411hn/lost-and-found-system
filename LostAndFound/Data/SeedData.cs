using LostAndFound.Models;
using LostAndFound.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LostAndFound.Data;

/// <summary>
/// Foundation seeding: the 4 roles + Starter Users (Admin, Staff, Member) for FR-AUTH-04.
/// Idempotent (safe to run on every startup).
/// </summary>
public static class SeedData
{
    public static readonly string[] Roles = { "Guest", "Member", "Staff", "Admin" };

    private const string AdminEmail = "admin@lostandfound.local";
    private const string AdminPassword = "Admin#12345";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        // ==========================================
        // Starter Admin
        // ==========================================
        if (await userManager.FindByEmailAsync(AdminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                FullName = "System Administrator"
            };

            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // ==========================================
        // Sample Staff account (FR-AUTH-04)
        // ==========================================
        string staffEmail = "staff@lostandfound.local";
        if (await userManager.FindByEmailAsync(staffEmail) == null)
        {
            var staff = new ApplicationUser
            {
                UserName = staffEmail,
                Email = staffEmail,
                EmailConfirmed = true,
                FullName = "Front-desk Staff"
            };

            var resultStaff = await userManager.CreateAsync(staff, "Staff#12345");
            if (resultStaff.Succeeded)
            {
                await userManager.AddToRoleAsync(staff, "Staff");
            }
        }

        // ==========================================
        // Sample Member account (FR-AUTH-04)
        // ==========================================
        string memberEmail = "member@lostandfound.local";
        if (await userManager.FindByEmailAsync(memberEmail) == null)
        {
            var member = new ApplicationUser
            {
                UserName = memberEmail,
                Email = memberEmail,
                EmailConfirmed = true,
                FullName = "Sample Member"
            };

            var resultMember = await userManager.CreateAsync(member, "Member#12345");
            if (resultMember.Succeeded)
            {
                await userManager.AddToRoleAsync(member, "Member");
            }
        }

        // ==========================================
        // Sample master data (FR-AUTH-04): 2-level Category + Location.
        // Needed by the FR-FOUND report form dropdowns. Idempotent.
        // ==========================================
        var db = sp.GetRequiredService<ApplicationDbContext>();

        if (!await db.Category.AnyAsync())
        {
            var electronics = new Category { Name = "Điện tử" };
            var papers = new Category { Name = "Giấy tờ" };
            var personal = new Category { Name = "Đồ dùng cá nhân" };
            var other = new Category { Name = "Khác" };
            db.Category.AddRange(electronics, papers, personal, other);
            await db.SaveChangesAsync(); // assign parent Ids

            db.Category.AddRange(
                new Category { Name = "Điện thoại", ParentId = electronics.Id },
                new Category { Name = "Laptop", ParentId = electronics.Id },
                new Category { Name = "Tai nghe", ParentId = electronics.Id },
                new Category { Name = "Thẻ sinh viên", ParentId = papers.Id },
                new Category { Name = "CCCD", ParentId = papers.Id },
                new Category { Name = "Ví", ParentId = papers.Id },
                new Category { Name = "Bình nước", ParentId = personal.Id },
                new Category { Name = "Ô (dù)", ParentId = personal.Id },
                new Category { Name = "Chìa khoá", ParentId = personal.Id });
            await db.SaveChangesAsync();
        }

        if (!await db.Location.AnyAsync())
        {
            db.Location.AddRange(
                new Location { Name = "Thư viện" },
                new Location { Name = "Căng tin" },
                new Location { Name = "Sảnh A" },
                new Location { Name = "Sân trường" },
                new Location { Name = "Phòng bảo vệ" });
            await db.SaveChangesAsync();
        }
    }
}
