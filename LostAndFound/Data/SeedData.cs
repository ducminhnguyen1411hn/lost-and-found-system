using LostAndFound.Models;
using Microsoft.AspNetCore.Identity;
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
    }
}
