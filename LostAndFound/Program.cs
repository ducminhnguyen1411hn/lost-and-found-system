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

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services
                .AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

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

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();

            builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
            builder.Services.AddSingleton(sp =>
            {
                var s = sp.GetRequiredService<IOptions<CloudinarySettings>>().Value;
                var cloudinary = new Cloudinary(new Account(s.CloudName, s.ApiKey, s.ApiSecret));
                cloudinary.Api.Secure = true;
                return cloudinary;
            });

            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<IAuditService, AuditService>();

            builder.Services.AddScoped<CloudinaryImageUploadService>();
            builder.Services.AddScoped<LocalImageUploadService>();
            builder.Services.AddScoped<IImageUploadService, FallbackImageUploadService>();
            builder.Services.AddScoped<IFoundItemService, FoundItemService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<ILostItemService, LostItemService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<INotificationQueries, NotificationService>();
            builder.Services.AddScoped<IClaimService, ClaimService>();
            builder.Services.AddScoped<IItemBoardService, ItemBoardService>();
            builder.Services.AddScoped<IHoldingService, HoldingService>();
            builder.Services.AddScoped<IUnclaimedSweepService, UnclaimedSweepService>();
            builder.Services.AddHostedService<UnclaimedSweepBackgroundService>();
            builder.Services.AddScoped<ICameraService, CameraService>();

            var app = builder.Build();

            await SeedData.InitializeAsync(app.Services);

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
                pattern: "{controller=Items}/{action=Index}/{id?}");
            app.MapRazorPages();

            await app.RunAsync();
        }
    }
}