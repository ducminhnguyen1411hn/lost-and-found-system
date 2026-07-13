using LostAndFound.Models;
using LostAndFound.Models.Entities;
using LostAndFound.Models.Enums;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Data;

/// <summary>
/// Demo dataset for local testing / presentation: ~100 found-item posts across the Vietnamese
/// categories, spread over the last two months, with generated members, tags, multiple images
/// (real photos via picsum.photos), and matching AuditLog rows. All times are UTC (project convention).
/// Deterministic (fixed RNG seed) and idempotent — it only runs when there are no items yet.
/// </summary>
public static class SeedDemoData
{
    private const int PostCount = 100;
    private const int UserCount = 20;
    private const string DemoPassword = "Demo#12345";

    // (Title, Description, leaf-category name, tags). Category names must match the tree in SeedData.
    private static readonly (string Title, string Desc, string Cat, string[] Tags)[] Archetypes =
    {
        ("Ví da nam", "Ví da nam, bên trong còn vài loại thẻ.", "Ví/Bóp", new[] { "ví", "ví da" }),
        ("iPhone 13", "Điện thoại đã khoá màn hình, còn pin.", "Điện thoại", new[] { "iphone", "điện thoại", "apple" }),
        ("Samsung Galaxy", "Điện thoại Android màn hình lớn.", "Điện thoại", new[] { "samsung", "android", "điện thoại" }),
        ("Thẻ sinh viên", "Thẻ SV có ảnh và mã số sinh viên.", "Thẻ sinh viên", new[] { "thẻ sinh viên", "mssv" }),
        ("Chùm chìa khoá xe máy", "Chùm chìa khoá kèm móc treo.", "Chìa khoá", new[] { "chìa khoá", "xe máy" }),
        ("Tai nghe Bluetooth", "Tai nghe không dây trong hộp sạc.", "Tai nghe", new[] { "tai nghe", "bluetooth", "airpods" }),
        ("Balo laptop", "Balo có ngăn đựng laptop.", "Balo", new[] { "balo", "laptop" }),
        ("Kính cận gọng nhựa", "Kính cận đựng trong hộp.", "Kính mắt", new[] { "kính", "kính cận" }),
        ("Ô gấp", "Ô/dù gấp gọn.", "Ô/Dù", new[] { "ô", "dù" }),
        ("Bình giữ nhiệt", "Bình giữ nhiệt còn nước.", "Bình giữ nhiệt", new[] { "bình nước", "giữ nhiệt" }),
        ("Áo khoác gió", "Áo khoác dù nhẹ.", "Áo khoác", new[] { "áo khoác", "áo gió" }),
        ("CCCD gắn chip", "Căn cước công dân.", "CCCD/CMND", new[] { "cccd", "căn cước" }),
        ("Thẻ ATM", "Thẻ ngân hàng.", "Thẻ ngân hàng/ATM", new[] { "atm", "thẻ ngân hàng" }),
        ("Laptop Dell", "Laptop trong túi chống sốc.", "Laptop", new[] { "laptop", "dell" }),
        ("Máy tính bảng iPad", "iPad còn bao da.", "Máy tính bảng", new[] { "ipad", "máy tính bảng" }),
        ("Đồng hồ đeo tay", "Đồng hồ dây da.", "Đồng hồ đeo tay", new[] { "đồng hồ" }),
        ("Mũ bảo hiểm", "Nón bảo hiểm nửa đầu.", "Mũ/Nón", new[] { "mũ", "nón bảo hiểm" }),
        ("Sạc dự phòng", "Pin sạc dự phòng.", "Sạc & Cáp", new[] { "sạc dự phòng", "pin" }),
        ("Túi xách nữ", "Túi xách nhỏ.", "Túi xách", new[] { "túi xách" }),
        ("Sổ tay", "Sổ ghi chép bìa cứng.", "Sổ & Vở", new[] { "sổ tay", "vở" }),
        ("Chuột không dây", "Chuột Logitech.", "Chuột & Bàn phím", new[] { "chuột", "logitech" }),
        ("Khăn choàng len", "Khăn choàng ấm.", "Khăn choàng", new[] { "khăn" }),
        ("Giày thể thao", "Một chiếc giày thể thao.", "Giày/Dép", new[] { "giày" }),
        ("Bằng lái xe A1", "Giấy phép lái xe.", "Bằng lái xe", new[] { "bằng lái", "gplx" }),
        ("Apple Watch", "Đồng hồ thông minh.", "Đồng hồ thông minh", new[] { "apple watch", "smartwatch" }),
        ("Món đồ nhỏ", "Món đồ nhỏ nhặt được, chưa rõ chủ.", "Khác", new[] { "khác" }),
    };

    public static async Task SeedAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ITagService tagService)
    {
        if (await db.FoundItem.AnyAsync()) return; // already seeded

        var rng = new Random(20260714); // deterministic

        // 1) Demo members (reporters).
        var surnames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Đặng", "Bùi", "Đỗ", "Ngô", "Dương", "Lý" };
        var middles = new[] { "Văn", "Thị", "Hữu", "Đức", "Minh", "Ngọc", "Gia", "Quang", "Thanh", "Hoài" };
        var givens = new[] { "An", "Bình", "Chi", "Dũng", "Giang", "Hà", "Hải", "Hoa", "Hùng", "Khánh", "Lan", "Linh", "Mai", "Nam", "Ngọc", "Phúc", "Quân", "Quỳnh", "Sơn", "Thảo", "Trang", "Tuấn", "Vy", "Yến" };

        var reporterIds = new List<string>();
        for (int i = 1; i <= UserCount; i++)
        {
            var email = $"user{i:00}@lostandfound.local";
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null) { reporterIds.Add(existing.Id); continue; }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = $"{surnames[rng.Next(surnames.Length)]} {middles[rng.Next(middles.Length)]} {givens[rng.Next(givens.Length)]}",
                PhoneNumber = $"09{rng.Next(10000000, 99999999)}"
            };
            var res = await userManager.CreateAsync(user, DemoPassword);
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Member");
                reporterIds.Add(user.Id);
            }
        }
        if (reporterIds.Count == 0) return; // nothing to attach posts to

        // 2) Reference data.
        var cats = await db.Category.AsNoTracking().ToListAsync();
        var leafCats = cats.Where(c => !cats.Any(x => x.ParentId == c.Id)).ToList(); // childless = selectable leaf
        var catIdByName = leafCats.ToDictionary(c => c.Name, c => c.Id);
        var locationIds = await db.Location.AsNoTracking().Select(l => l.Id).ToListAsync();

        // 3) Resolve every tag once (creates the Tag rows), then map normalized -> id.
        await tagService.ResolveTagsAsync(Archetypes.SelectMany(a => a.Tags).Distinct());
        await db.SaveChangesAsync();
        var tagIdByNorm = await db.Tag.AsNoTracking().ToDictionaryAsync(t => t.NormalizedTag, t => t.Id);

        var colors = new[] { "đen", "trắng", "xanh", "đỏ", "xám", "be", "nâu", "hồng" };
        var now = DateTime.UtcNow;

        // 4) Create the posts.
        var created = new List<(FoundItem Item, string[] Tags, int Images)>();
        for (int i = 0; i < PostCount; i++)
        {
            var a = Archetypes[rng.Next(Archetypes.Length)];
            var catId = catIdByName.TryGetValue(a.Cat, out var cid) ? cid : leafCats[rng.Next(leafCats.Count)].Id;
            var foundAtUtc = now.AddDays(-rng.Next(0, 60)).AddHours(-rng.Next(0, 24)).AddMinutes(-rng.Next(0, 60));
            var custodial = rng.Next(100) < 15; // ~15% custodial (kept out of the public list)
            var status = custodial ? FoundItemStatus.PendingDropoff : FoundItemStatus.Open;

            var item = new FoundItem
            {
                Title = a.Title,
                Description = $"{a.Desc} Màu {colors[rng.Next(colors.Length)]}.",
                CategoryId = catId,
                LocationId = locationIds[rng.Next(locationIds.Count)],
                FoundAt = foundAtUtc,
                Status = (int)status,
                HoldingType = (int)(custodial ? HoldingType.Custodial : HoldingType.SelfHeld),
                PrivateMarks = rng.Next(2) == 0 ? "Có một đặc điểm riêng chỉ chủ mới biết." : null,
                ReporterUserId = reporterIds[rng.Next(reporterIds.Count)],
                CreatedAt = foundAtUtc.AddMinutes(rng.Next(5, 180))
            };
            db.FoundItem.Add(item);
            created.Add((item, a.Tags, rng.Next(1, 5))); // 1-4 images
        }
        await db.SaveChangesAsync(); // assign item Ids

        // 5) Images + tag links + audit rows.
        foreach (var (item, tags, images) in created)
        {
            for (int j = 0; j < images; j++)
                db.FoundItemImage.Add(new FoundItemImage
                {
                    FoundItemId = item.Id,
                    Url = $"https://picsum.photos/seed/laf{item.Id}_{j}/640/420",
                    SortOrder = j
                });

            var seenTagIds = new HashSet<int>();
            foreach (var t in tags)
                if (tagIdByNorm.TryGetValue(tagService.Normalize(t), out var tid) && seenTagIds.Add(tid))
                    db.FoundItemTag.Add(new FoundItemTag { FoundItemId = item.Id, TagId = tid });

            var status = (FoundItemStatus)item.Status;
            db.AuditLog.Add(new AuditLog
            {
                ActorUserId = item.ReporterUserId,
                Action = "Created",
                EntityType = "FoundItem",
                EntityId = item.Id.ToString(),
                ToStatus = status.ToString(),
                Detail = $"Đăng đồ nhặt được: {item.Title}",
                IsPublic = status == FoundItemStatus.Open,
                CreatedAt = item.CreatedAt
            });

            if (rng.Next(100) < 25) // ~1/4 also have an edit event
                db.AuditLog.Add(new AuditLog
                {
                    ActorUserId = item.ReporterUserId,
                    Action = "Updated",
                    EntityType = "FoundItem",
                    EntityId = item.Id.ToString(),
                    Detail = "Cập nhật bài đăng",
                    IsPublic = true,
                    CreatedAt = item.CreatedAt.AddHours(rng.Next(1, 48))
                });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Demo dataset for the LOST-item board: ~24 public "I lost this" posts by the demo members.
    /// Runs once, when there are no lost items yet. Reuses the same archetypes/tags.</summary>
    public static async Task SeedLostItemsAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ITagService tagService)
    {
        if (await db.LostItem.AnyAsync()) return;

        var owners = await db.Users.Where(u => u.Email != null && u.Email.StartsWith("user")).Select(u => u.Id).ToListAsync();
        if (owners.Count == 0) return;

        var cats = await db.Category.AsNoTracking().ToListAsync();
        var leafCats = cats.Where(c => !cats.Any(x => x.ParentId == c.Id)).ToList();
        var catIdByName = leafCats.ToDictionary(c => c.Name, c => c.Id);
        var locationIds = await db.Location.AsNoTracking().Select(l => l.Id).ToListAsync();

        await tagService.ResolveTagsAsync(Archetypes.SelectMany(a => a.Tags).Distinct());
        await db.SaveChangesAsync();
        var tagIdByNorm = await db.Tag.AsNoTracking().ToDictionaryAsync(t => t.NormalizedTag, t => t.Id);

        var rng = new Random(777);
        var now = DateTime.UtcNow;
        const int count = 24;

        var created = new List<(LostItem Item, string[] Tags, int Images)>();
        for (int i = 0; i < count; i++)
        {
            var a = Archetypes[rng.Next(Archetypes.Length)];
            var catId = catIdByName.TryGetValue(a.Cat, out var cid) ? cid : leafCats[rng.Next(leafCats.Count)].Id;
            var lostAt = now.AddDays(-rng.Next(0, 45)).AddHours(-rng.Next(0, 24)).AddMinutes(-rng.Next(0, 60));

            var item = new LostItem
            {
                Title = a.Title,
                Description = $"Mình làm mất {a.Title.ToLowerInvariant()}. {a.Desc}",
                CategoryId = catId,
                LocationId = locationIds[rng.Next(locationIds.Count)],
                LostAt = lostAt,
                Status = (int)LostItemStatus.Open,
                OwnerUserId = owners[rng.Next(owners.Count)],
                CreatedAt = lostAt.AddMinutes(rng.Next(5, 180))
            };
            db.LostItem.Add(item);
            created.Add((item, a.Tags, rng.Next(0, 3))); // 0-2 images (losers often have none)
        }
        await db.SaveChangesAsync();

        foreach (var (item, tags, images) in created)
        {
            for (int j = 0; j < images; j++)
                db.LostItemImage.Add(new LostItemImage
                {
                    LostItemId = item.Id,
                    Url = $"https://picsum.photos/seed/laflost{item.Id}_{j}/640/420",
                    SortOrder = j
                });

            var seen = new HashSet<int>();
            foreach (var t in tags)
                if (tagIdByNorm.TryGetValue(tagService.Normalize(t), out var tid) && seen.Add(tid))
                    db.LostItemTag.Add(new LostItemTag { LostItemId = item.Id, TagId = tid });

            db.AuditLog.Add(new AuditLog
            {
                ActorUserId = item.OwnerUserId,
                Action = "Created",
                EntityType = "LostItem",
                EntityId = item.Id.ToString(),
                ToStatus = LostItemStatus.Open.ToString(),
                Detail = $"Đăng đồ bị mất: {item.Title}",
                IsPublic = true,
                CreatedAt = item.CreatedAt
            });
        }
        await db.SaveChangesAsync();
    }
}
