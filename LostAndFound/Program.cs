using LostAndFound.Data;
using LostAndFound.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ---- Database (DB-First; the schema lives in db/schema.sql) ----
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // ---- ASP.NET Core Identity (role-based: Guest / Member / Staff / Admin) ----
            builder.Services
                .AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false; // simplest for a campus app
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // ---- OAuth 2.0 Authentication (Google) ----
            builder.Services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
                        ?? throw new InvalidOperationException("Google ClientId not found in configuration.");
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
                        ?? throw new InvalidOperationException("Google ClientSecret not found in configuration.");
                });

            // ---- MVC + Razor Pages (default Identity UI) + SignalR ----
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();

            // ---- Feature services: register when implemented (left commented on purpose) ----
            // builder.Services.AddScoped<ITagService, TagService>();
            // builder.Services.AddScoped<INotificationService, NotificationService>();
            // builder.Services.AddScoped<IAuditService, AuditService>();

            var app = builder.Build();

            // Seed the 4 roles + a starter Admin account (idempotent).
            await SeedData.InitializeAsync(app.Services);

            // ---- HTTP request pipeline ----
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication(); // must come before UseAuthorization
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages(); // serves the default Identity UI (/Identity/Account/Login etc.)
            // app.MapHub<YourHub>("/hubs/your-hub"); // add when the first SignalR hub lands

            await app.RunAsync();
        }
    }
}