using LostAndFound.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LostAndFound.Data;

/// <summary>
/// Minimal foundation seeding: the 4 roles + one starter Admin so the app is usable immediately.
/// Idempotent (safe to run on every startup). Fuller seed data (sample members, categories,
/// locations) belongs to the FR-AUTH-04 feature, not the base.
/// </summary>
public static class SeedData
{
    public static readonly string[] Roles = { "Guest", "Member", "Staff", "Admin" };

    // Starter admin — documented in docs/INDEX.md. Change the password before any real deployment.
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
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                FullName = "System Administrator",
                Department = "IT"
            };

            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
