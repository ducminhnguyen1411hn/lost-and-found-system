using LostAndFound.Models;
using LostAndFound.Models.Entities;
using LostAndFound.Services.Interfaces;
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
            // 2-level Vietnamese category tree. A parent with no children (e.g. "Khác") is itself selectable.
            var tree = new (string Parent, string[] Children)[]
            {
                ("Điện tử", new[] { "Điện thoại", "Laptop", "Máy tính bảng", "Tai nghe", "Sạc & Cáp", "Chuột & Bàn phím", "Đồng hồ thông minh" }),
                ("Giấy tờ", new[] { "Thẻ sinh viên", "CCCD/CMND", "Bằng lái xe", "Thẻ ngân hàng/ATM", "Sổ & Vở" }),
                ("Ví & Túi", new[] { "Ví/Bóp", "Túi xách", "Balo", "Túi đựng laptop" }),
                ("Đồ dùng cá nhân", new[] { "Chìa khoá", "Kính mắt", "Ô/Dù", "Bình giữ nhiệt", "Mũ/Nón", "Đồng hồ đeo tay" }),
                ("Trang phục", new[] { "Áo khoác", "Khăn choàng", "Giày/Dép" }),
                ("Khác", Array.Empty<string>()),
            };
            foreach (var (parent, children) in tree)
            {
                var p = new Category { Name = parent };
                db.Category.Add(p);
                await db.SaveChangesAsync(); // assign parent Id
                foreach (var ch in children)
                    db.Category.Add(new Category { Name = ch, ParentId = p.Id });
            }
            await db.SaveChangesAsync();
        }

        if (!await db.Location.AnyAsync())
        {
            db.Location.AddRange(
                new Location { Name = "Thư viện trung tâm" },
                new Location { Building = "Toà A", Name = "Căng tin" },
                new Location { Building = "Toà H1", Name = "Giảng đường 101" },
                new Location { Building = "Toà H2", Name = "Giảng đường 205" },
                new Location { Name = "Sảnh chính" },
                new Location { Name = "Bãi giữ xe" },
                new Location { Name = "Sân vận động" },
                new Location { Name = "Ký túc xá khu B" },
                new Location { Name = "Phòng bảo vệ" },
                new Location { Name = "Hội trường lớn" },
                new Location { Name = "Phòng thí nghiệm B4" },
                new Location { Name = "Nhà xe sinh viên" });
            await db.SaveChangesAsync();
        }

        // Demo dataset (~100 posts + users + images + tags + audit). Runs once, when there are no items.
        await SeedDemoData.SeedAsync(db, userManager, sp.GetRequiredService<ITagService>());
    }
}
