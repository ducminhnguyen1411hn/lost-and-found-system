using LostAndFound.Models;
using LostAndFound.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Category => Set<Category>();
    public DbSet<Location> Location => Set<Location>();
    public DbSet<Tag> Tag => Set<Tag>();
    public DbSet<LostItem> LostItem => Set<LostItem>();
    public DbSet<LostItemImage> LostItemImage => Set<LostItemImage>();
    public DbSet<LostItemTag> LostItemTag => Set<LostItemTag>();
    public DbSet<FoundItem> FoundItem => Set<FoundItem>();
    public DbSet<FoundItemImage> FoundItemImage => Set<FoundItemImage>();
    public DbSet<FoundItemTag> FoundItemTag => Set<FoundItemTag>();
    public DbSet<Claim> Claim => Set<Claim>();
    public DbSet<ClaimImage> ClaimImage => Set<ClaimImage>();
    public DbSet<ClaimMessage> ClaimMessage => Set<ClaimMessage>();
    public DbSet<CameraCheckRequest> CameraCheckRequest => Set<CameraCheckRequest>();
    public DbSet<Notification> Notification => Set<Notification>();
    public DbSet<AuditLog> AuditLog => Set<AuditLog>();

    public new DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.ActorUserId, "IX_AuditLog_ActorUserId");
            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "IX_AuditLog_Entity");
            entity.HasIndex(e => e.IsPublic, "IX_AuditLog_IsPublic");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Detail).HasMaxLength(2000);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.FromStatus).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.ToStatus).HasMaxLength(50);

            entity.HasOne(d => d.ActorUser).WithMany()
                .HasForeignKey(d => d.ActorUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CameraCheckRequest>(entity =>
        {
            entity.HasIndex(e => e.HandledByStaffId, "IX_CameraCheckRequest_HandledByStaffId");
            entity.HasIndex(e => e.LocationId, "IX_CameraCheckRequest_LocationId");
            entity.HasIndex(e => e.RequesterUserId, "IX_CameraCheckRequest_RequesterUserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ItemDescription).HasMaxLength(1000);
            entity.Property(e => e.ResponseNote).HasMaxLength(1000);

            entity.HasOne(d => d.Location).WithMany(p => p.CameraCheckRequest)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.ParentId, "IX_Category_ParentId");

            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent).HasForeignKey(d => d.ParentId);
        });

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.HasIndex(e => e.ClaimantUserId, "IX_Claim_ClaimantUserId");
            entity.HasIndex(e => e.FoundItemId, "IX_Claim_FoundItemId");
            entity.HasIndex(e => e.HandledByUserId, "IX_Claim_HandledByUserId");

            entity.Property(e => e.ContactEmail).HasMaxLength(256);
            entity.Property(e => e.ContactPhone).HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RejectReason).HasMaxLength(1000);
            entity.Property(e => e.VerificationDetails).HasMaxLength(2000);

            entity.HasOne(d => d.FoundItem).WithMany(p => p.Claim)
                .HasForeignKey(d => d.FoundItemId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ClaimImage>(entity =>
        {
            entity.HasIndex(e => e.ClaimId, "IX_ClaimImage_ClaimId");

            entity.Property(e => e.Url).HasMaxLength(400);

            entity.HasOne(d => d.Claim).WithMany(p => p.ClaimImage).HasForeignKey(d => d.ClaimId);
        });

        modelBuilder.Entity<ClaimMessage>(entity =>
        {
            entity.HasIndex(e => e.ClaimId, "IX_ClaimMessage_ClaimId");

            entity.Property(e => e.Body).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SenderUserId).HasMaxLength(450);

            entity.HasOne(d => d.Claim).WithMany(p => p.ClaimMessage).HasForeignKey(d => d.ClaimId);
        });

        modelBuilder.Entity<FoundItem>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_FoundItem_CategoryId");
            entity.HasIndex(e => e.CustodianStaffId, "IX_FoundItem_CustodianStaffId");
            entity.HasIndex(e => e.LocationId, "IX_FoundItem_LocationId");
            entity.HasIndex(e => e.ReporterUserId, "IX_FoundItem_ReporterUserId");
            entity.HasIndex(e => e.Status, "IX_FoundItem_Status");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.PrivateMarks).HasMaxLength(1000);

            entity.Property(e => e.StorageLocation).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Category).WithMany(p => p.FoundItem)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Location).WithMany(p => p.FoundItem)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ReporterUser).WithMany()
                .HasForeignKey(d => d.ReporterUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.CustodianStaff).WithMany()
                .HasForeignKey(d => d.CustodianStaffId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<FoundItemImage>(entity =>
        {
            entity.HasIndex(e => e.FoundItemId, "IX_FoundItemImage_FoundItemId");

            entity.Property(e => e.Url).HasMaxLength(400);

            entity.HasOne(d => d.FoundItem).WithMany(p => p.FoundItemImage).HasForeignKey(d => d.FoundItemId);
        });

        modelBuilder.Entity<FoundItemTag>(entity =>
        {
            entity.HasIndex(e => e.TagId, "IX_FoundItemTag_TagId");
            entity.HasIndex(e => new { e.FoundItemId, e.TagId }, "UX_FoundItemTag_Item_Tag").IsUnique();

            entity.HasOne(d => d.FoundItem).WithMany(p => p.FoundItemTag).HasForeignKey(d => d.FoundItemId);

            entity.HasOne(d => d.Tag).WithMany(p => p.FoundItemTag)
                .HasForeignKey(d => d.TagId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.Property(e => e.Building).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(150);
        });

        modelBuilder.Entity<LostItem>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_LostItem_CategoryId");
            entity.HasIndex(e => e.LocationId, "IX_LostItem_LocationId");
            entity.HasIndex(e => e.OwnerUserId, "IX_LostItem_OwnerUserId");
            entity.HasIndex(e => e.Status, "IX_LostItem_Status");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Category).WithMany(p => p.LostItem)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Location).WithMany(p => p.LostItem)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<LostItemImage>(entity =>
        {
            entity.HasIndex(e => e.LostItemId, "IX_LostItemImage_LostItemId");

            entity.Property(e => e.Url).HasMaxLength(400);

            entity.HasOne(d => d.LostItem).WithMany(p => p.LostItemImage).HasForeignKey(d => d.LostItemId);
        });

        modelBuilder.Entity<LostItemTag>(entity =>
        {
            entity.HasIndex(e => e.TagId, "IX_LostItemTag_TagId");
            entity.HasIndex(e => new { e.LostItemId, e.TagId }, "UX_LostItemTag_Item_Tag").IsUnique();

            entity.HasOne(d => d.LostItem).WithMany(p => p.LostItemTag).HasForeignKey(d => d.LostItemId);

            entity.HasOne(d => d.Tag).WithMany(p => p.LostItemTag)
                .HasForeignKey(d => d.TagId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.RecipientUserId, e.IsRead }, "IX_Notification_Recipient_IsRead");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LinkUrl).HasMaxLength(400);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(100);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasIndex(e => e.NormalizedTag, "UX_Tag_NormalizedTag").IsUnique();

            entity.Property(e => e.DisplayTag).HasMaxLength(100);
            entity.Property(e => e.NormalizedTag).HasMaxLength(100);
        });
    }
}
