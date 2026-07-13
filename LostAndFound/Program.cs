using CloudinaryDotNet;
using LostAndFound.Data;
using LostAndFound.Models;
using LostAndFound.Models.Settings;
using LostAndFound.Services;
using LostAndFound.Services.Images;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // ---- OAuth 2.0 Authentication (Google) ----
            // Register Google login ONLY when its credentials are configured (user-secrets / env / appsettings).
            // The old code threw inside the options builder when the ClientId was missing; AuthenticationMiddleware
            // builds that handler on EVERY request, so a missing secret returned HTTP 500 across the whole app.
            // Skipping registration when unconfigured lets the app run fine (just without the Google login button).
            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                builder.Services.AddAuthentication()
                    .AddGoogle(options =>
                    {
                        options.ClientId = googleClientId;
                        options.ClientSecret = googleClientSecret;
                    });
            }

            // ---- MVC + Razor Pages + SignalR ----
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();

            // ---- Cloudinary (found-item images, FR-FOUND-05) ----
            builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
            builder.Services.AddSingleton(sp =>
            {
                var s = sp.GetRequiredService<IOptions<CloudinarySettings>>().Value;
                var cloudinary = new Cloudinary(new Account(s.CloudName, s.ApiKey, s.ApiSecret));
                cloudinary.Api.Secure = true;
                return cloudinary;
            });

            // ---- Domain services (first vertical slice wires the shared contracts) ----
            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<IAuditService, AuditService>();
            builder.Services.AddScoped<IImageUploadService, CloudinaryImageUploadService>();
            builder.Services.AddScoped<IFoundItemService, FoundItemService>();

            var app = builder.Build();

            // Seed the 4 roles + starter Admin account
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=FoundItems}/{action=Index}/{id?}");
            app.MapRazorPages();

            await app.RunAsync();
        }
    }
}