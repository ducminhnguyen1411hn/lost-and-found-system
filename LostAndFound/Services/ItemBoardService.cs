using LostAndFound.Data;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Common;
using LostAndFound.Models.ViewModels.Items;
using LostAndFound.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

/// <summary>
/// The unified board (found + lost in one list). The two sides live in different tables, so the union
/// happens in SQL over a SCALAR-ONLY projection (<see cref="BoardRow"/>): collections/correlated
/// subqueries inside a set operation don't translate, so tags and images are hydrated afterwards for
/// just the current page's rows. Public visibility differs per side and must NOT be unified:
/// FoundItemStatus.Open = 1 but LostItemStatus.Open = 0.
/// </summary>
public class ItemBoardService : IItemBoardService
{
    private const int MaxPageSize = 60;

    private readonly ApplicationDbContext _db;
    private readonly ITagService _tags;

    public ItemBoardService(ApplicationDbContext db, ITagService tags)
    {
        _db = db;
        _tags = tags;
    }

    /// <summary>Union row — scalars only (column reads + joins). Nothing here may be a collection
    /// or a correlated subquery, or the Concat below stops translating.</summary>
    private sealed class BoardRow
    {
        public int Id { get; set; }
        public int Kind { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Status { get; set; }
    }

    /// <inheritdoc />
    public async Task<PagedResult<BoardItemViewModel>> SearchAsync(BoardSearchViewModel q, string? ownerUserId = null)
    {
        // null = public board (Open only, all owners). Non-null = that user's own posts, every status.
        // See IItemBoardService: this is deliberately NOT model-bound.
        var mine = ownerUserId is not null;
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize < 1 ? 12 : Math.Min(q.PageSize, MaxPageSize);

        var kw = string.IsNullOrWhiteSpace(q.Keyword) ? null : q.Keyword.Trim();
        var norm = string.IsNullOrWhiteSpace(q.Tag) ? null : _tags.Normalize(q.Tag);
        DateTime? fromUtc = q.From is DateTime f ? AppTime.ToUtc(f) : null;
        DateTime? toUtc = q.To is DateTime t ? AppTime.ToUtc(t).AddMinutes(1) : null; // inclusive of the "to" minute

        // Category is a 2-level tree and items are filed on the LEAF, so a parent almost always holds zero
        // items of its own. Matching CategoryId exactly made picking a parent ("Ví & Túi") return 0 results
        // while its children (Ví/Bóp, Túi xách, Balo) held them all. Resolve self + children once here, then
        // match with Contains on both branches. Depth is fixed at 2, so one level of children is enough.
        List<int>? catIds = null;
        if (q.CategoryId is int cat)
            catIds = await _db.Category.AsNoTracking()
                .Where(c => c.Id == cat || c.ParentId == cat)
                .Select(c => c.Id)
                .ToListAsync();

        IQueryable<BoardRow>? union = null;

        if (q.Kind != ItemKind.Lost) // "Tất cả" or "Đồ nhặt được"
        {
            var fq = _db.FoundItem.AsNoTracking();
            // Owner sees every status of their own posts; the public sees only Open.
            fq = mine
                ? fq.Where(x => x.ReporterUserId == ownerUserId)
                : fq.Where(x => x.Status == (int)FoundItemStatus.Open);
            if (kw != null) fq = fq.Where(x => x.Title.Contains(kw) || (x.Description != null && x.Description.Contains(kw)));
            if (catIds != null) fq = fq.Where(x => catIds.Contains(x.CategoryId));
            if (q.LocationId is int l) fq = fq.Where(x => x.LocationId == l);
            if (fromUtc is DateTime a) fq = fq.Where(x => x.FoundAt >= a);
            if (toUtc is DateTime b) fq = fq.Where(x => x.FoundAt < b);
            if (norm != null) fq = fq.Where(x => x.FoundItemTag.Any(ft => ft.Tag.NormalizedTag == norm));

            union = fq.Select(x => new BoardRow
            {
                Id = x.Id,
                Kind = (int)ItemKind.Found,
                Title = x.Title,
                CategoryName = x.Category.Name,
                LocationName = x.Location.Name,
                OccurredAt = x.FoundAt,
                CreatedAt = x.CreatedAt,
                Status = x.Status
            });
        }

        if (q.Kind != ItemKind.Found) // "Tất cả" or "Đồ bị mất"
        {
            var lq = _db.LostItem.AsNoTracking();
            // NB: LostItemStatus.Open = 0 while FoundItemStatus.Open = 1 — the two scales differ.
            lq = mine
                ? lq.Where(x => x.OwnerUserId == ownerUserId)
                : lq.Where(x => x.Status == (int)LostItemStatus.Open);
            if (kw != null) lq = lq.Where(x => x.Title.Contains(kw) || (x.Description != null && x.Description.Contains(kw)));
            if (catIds != null) lq = lq.Where(x => catIds.Contains(x.CategoryId));
            if (q.LocationId is int l) lq = lq.Where(x => x.LocationId == l);
            if (fromUtc is DateTime a) lq = lq.Where(x => x.LostAt >= a);
            if (toUtc is DateTime b) lq = lq.Where(x => x.LostAt < b);
            if (norm != null) lq = lq.Where(x => x.LostItemTag.Any(lt => lt.Tag.NormalizedTag == norm));

            var lRows = lq.Select(x => new BoardRow
            {
                Id = x.Id,
                Kind = (int)ItemKind.Lost,
                Title = x.Title,
                CategoryName = x.Category.Name,
                LocationName = x.Location.Name,
                OccurredAt = x.LostAt,
                CreatedAt = x.CreatedAt,
                Status = x.Status
            });

            union = union is null ? lRows : union.Concat(lRows);
        }

        if (union is null)
            return new PagedResult<BoardItemViewModel> { Items = new List<BoardItemViewModel>(), Page = page, PageSize = pageSize, TotalCount = 0 };

        var total = await union.CountAsync();

        // ThenBy(Id) keeps pagination deterministic when two rows share CreatedAt across the union.
        var rows = await union
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var foundIds = rows.Where(r => r.Kind == (int)ItemKind.Found).Select(r => r.Id).ToList();
        var lostIds = rows.Where(r => r.Kind == (int)ItemKind.Lost).Select(r => r.Id).ToList();

        // Hydrate tags + cover/count for THIS PAGE only (small IN-lists), not for the whole result set.
        // NOTE: declared with an explicit type (not `var`) so the two ternary branches — an empty
        // List<(int Id, ...)> literal vs. a projected .Select(x => (x.FoundItemId, ...)).ToList() —
        // unify via tuple identity conversion despite their differing inferred element names.
        List<(int Id, string Tag)> fTags = foundIds.Count == 0
            ? new List<(int Id, string Tag)>()
            : (await _db.FoundItemTag.AsNoTracking().Where(ft => foundIds.Contains(ft.FoundItemId))
                .Select(ft => new { ft.FoundItemId, ft.Tag.DisplayTag }).ToListAsync())
                .Select(x => (x.FoundItemId, x.DisplayTag)).ToList();
        List<(int Id, string Tag)> lTags = lostIds.Count == 0
            ? new List<(int Id, string Tag)>()
            : (await _db.LostItemTag.AsNoTracking().Where(lt => lostIds.Contains(lt.LostItemId))
                .Select(lt => new { lt.LostItemId, lt.Tag.DisplayTag }).ToListAsync())
                .Select(x => (x.LostItemId, x.DisplayTag)).ToList();

        List<(int Id, string Url, int SortOrder)> fImgs = foundIds.Count == 0
            ? new List<(int Id, string Url, int SortOrder)>()
            : (await _db.FoundItemImage.AsNoTracking().Where(i => foundIds.Contains(i.FoundItemId))
                .Select(i => new { i.FoundItemId, i.Url, i.SortOrder }).ToListAsync())
                .Select(x => (x.FoundItemId, x.Url, x.SortOrder)).ToList();
        List<(int Id, string Url, int SortOrder)> lImgs = lostIds.Count == 0
            ? new List<(int Id, string Url, int SortOrder)>()
            : (await _db.LostItemImage.AsNoTracking().Where(i => lostIds.Contains(i.LostItemId))
                .Select(i => new { i.LostItemId, i.Url, i.SortOrder }).ToListAsync())
                .Select(x => (x.LostItemId, x.Url, x.SortOrder)).ToList();

        var items = rows.Select(r =>
        {
            var isFound = r.Kind == (int)ItemKind.Found;
            var imgs = (isFound ? fImgs : lImgs).Where(i => i.Id == r.Id).OrderBy(i => i.SortOrder).ToList();
            var tags = (isFound ? fTags : lTags).Where(t => t.Id == r.Id).Select(t => t.Tag).ToList();
            return new BoardItemViewModel
            {
                Id = r.Id,
                Kind = (ItemKind)r.Kind,
                Title = r.Title,
                CategoryName = r.CategoryName,
                LocationName = r.LocationName,
                OccurredAt = r.OccurredAt,
                CreatedAt = r.CreatedAt,
                CoverImagePath = imgs.Count > 0 ? imgs[0].Url : null,
                ImageCount = imgs.Count,
                DisplayTags = tags,
                StatusRaw = r.Status
            };
        }).ToList();

        return new PagedResult<BoardItemViewModel> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }
}
