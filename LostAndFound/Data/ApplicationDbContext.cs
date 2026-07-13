using LostAndFound.Models;
using LostAndFound.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Data;

/// <summary>
/// The single runtime EF Core context. It IS an <see cref="IdentityDbContext{TUser}"/> (so all
/// AspNet* Identity tables + role logic work out of the box) AND hosts the domain entities that
/// were generated from the database into <c>Models/Entities</c> by the DB-First scaffold.
///
/// HAND-WRITTEN and authoritative. When the schema changes: edit <c>db/schema.sql</c>, recreate the
/// DB, re-run the scaffold (see <c>Data/Scaffolded/README.md</c>), then copy any new DbSets / config
/// from the regenerated ScaffoldDbContext into the OnModelCreating block below.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ---- Domain tables (entities are generated; these DbSets are hand-declared) ----
    public DbSet<Category> Category => Set<Category>();
    public DbSet<Location> Location => Set<Location>();
    public DbSet<Tag> Tag => Set<Tag>();
    public DbSet<LostAlert> LostAlert => Set<LostAlert>();
    public DbSet<LostAlertTag> LostAlertTag => Set<LostAlertTag>();
    public DbSet<FoundItem> FoundItem => Set<FoundItem>();
    public DbSet<FoundItemImage> FoundItemImage => Set<FoundItemImage>();
    public DbSet<FoundItemTag> FoundItemTag => Set<FoundItemTag>();
    public DbSet<Claim> Claim => Set<Claim>();
    public DbSet<CameraCheckRequest> CameraCheckRequest => Set<CameraCheckRequest>();
    public DbSet<ThankYou> ThankYou => Set<ThankYou>();
    public DbSet<Notification> Notification => Set<Notification>();
    public DbSet<AuditLog> AuditLog => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity table mapping MUST run first.
        base.OnModelCreating(modelBuilder);

        // ===================================================================
        // Domain config — copied verbatim from the DB-First scaffold output
        // (Data/Scaffolded/ScaffoldDbContext.cs). Keep in sync on re-scaffold.
        // FKs to AspNetUsers are intentionally plain scalar columns (no nav),
        // because the user tables are excluded from the scaffold selection set.
        // ===================================================================

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

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EvidenceImagePath).HasMaxLength(400);
            entity.Property(e => e.RejectReason).HasMaxLength(1000);
            entity.Property(e => e.VerificationDetails).HasMaxLength(2000);

            entity.HasOne(d => d.FoundItem).WithMany(p => p.Claim)
                .HasForeignKey(d => d.FoundItemId)
                .OnDelete(DeleteBehavior.ClientSetNull);
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
            // NOTE: intentionally NOT HasDefaultValue(1). With an EF store-default, EF treats the CLR
            // default (0 == PendingDropoff, a real Custodial status) as "unset" and omits it on INSERT,
            // so Custodial items would wrongly persist as Open. The service always sets Status explicitly;
            // the DB column default 1 still guards raw inserts. Keep this removed after any re-scaffold.
            entity.Property(e => e.StorageLocation).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Category).WithMany(p => p.FoundItem)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Location).WithMany(p => p.FoundItem)
                .HasForeignKey(d => d.LocationId)
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

        modelBuilder.Entity<LostAlert>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_LostAlert_CategoryId");
            entity.HasIndex(e => e.LocationId, "IX_LostAlert_LocationId");
            entity.HasIndex(e => e.OwnerUserId, "IX_LostAlert_OwnerUserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Keyword).HasMaxLength(200);

            entity.HasOne(d => d.Category).WithMany(p => p.LostAlert).HasForeignKey(d => d.CategoryId);
            entity.HasOne(d => d.Location).WithMany(p => p.LostAlert).HasForeignKey(d => d.LocationId);
        });

        modelBuilder.Entity<LostAlertTag>(entity =>
        {
            entity.HasIndex(e => e.TagId, "IX_LostAlertTag_TagId");
            entity.HasIndex(e => new { e.LostAlertId, e.TagId }, "UX_LostAlertTag_Alert_Tag").IsUnique();

            entity.HasOne(d => d.LostAlert).WithMany(p => p.LostAlertTag).HasForeignKey(d => d.LostAlertId);

            entity.HasOne(d => d.Tag).WithMany(p => p.LostAlertTag)
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

        modelBuilder.Entity<ThankYou>(entity =>
        {
            entity.HasIndex(e => e.FromUserId, "IX_ThankYou_FromUserId");
            entity.HasIndex(e => e.ToUserId, "IX_ThankYou_ToUserId");
            entity.HasIndex(e => e.FoundItemId, "UX_ThankYou_FoundItemId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Message).HasMaxLength(1000);

            entity.HasOne(d => d.FoundItem).WithOne(p => p.ThankYou)
                .HasForeignKey<ThankYou>(d => d.FoundItemId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}
