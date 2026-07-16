using System.Security.Claims;
using LostAndFound.Data;
using LostAndFound.Models.Entities;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Claims;
using LostAndFound.Models.ViewModels.Common;
using LostAndFound.Services.Images;
using LostAndFound.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

/// <summary>All FR-CLAIM rules & transitions. Each mutation = one transaction (change + AuditLog +
/// Notification, atomic). Blind listing enforced by construction. See IClaimService.</summary>
public class ClaimService : IClaimService
{
    private const string EvidenceFolder = "lostandfound/claims";
    private const int MaxEvidence = 5;

    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly INotificationService _notify;
    private readonly IImageUploadService _images;

    public ClaimService(ApplicationDbContext db, IAuditService audit,
                        INotificationService notify, IImageUploadService images)
    {
        _db = db;
        _audit = audit;
        _notify = notify;
        _images = images;
    }

    private static string? Uid(ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Holder = reporter if SelfHeld, custodian staff if Custodial. Admin always allowed to act.</summary>
    private static bool IsHolderOrAdmin(FoundItem item, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        if (uid is null) return false;
        if (user.IsInRole("Admin")) return true;
        return (HoldingType)item.HoldingType == HoldingType.SelfHeld
            ? uid == item.ReporterUserId
            : uid == item.CustodianStaffId;
    }

    private static bool IsHolder(FoundItem item, string uid) =>
        (HoldingType)item.HoldingType == HoldingType.SelfHeld
            ? uid == item.ReporterUserId
            : uid == item.CustodianStaffId;

    private Task<string> NameOf(string userId) =>
        _db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => u.FullName ?? u.Email!).FirstOrDefaultAsync()!;

    private static string ItemLink(int id) => $"/FoundItems/Details/{id}";

    public async Task<ClaimCreateViewModel?> GetCreateFormAsync(int foundItemId, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        if (uid is null) return null;

        var item = await _db.FoundItem.AsNoTracking()
            .Include(f => f.FoundItemImage)
            .FirstOrDefaultAsync(f => f.Id == foundItemId);
        if (item is null) return null;
        if ((FoundItemStatus)item.Status != FoundItemStatus.Open) return null;   // only Open is claimable
        if (uid == item.ReporterUserId || IsHolder(item, uid)) return null;      // can't claim your own/held item

        var hasActive = await _db.Claim.AsNoTracking().AnyAsync(c =>
            c.FoundItemId == foundItemId && c.ClaimantUserId == uid &&
            (c.Status == (int)ClaimStatus.Pending || c.Status == (int)ClaimStatus.Accepted));
        if (hasActive) return null;

        return new ClaimCreateViewModel
        {
            FoundItemId = item.Id,
            ItemTitle = item.Title,
            ItemCoverImage = item.FoundItemImage.OrderBy(im => im.SortOrder).Select(im => im.Url).FirstOrDefault()
        };
    }

    public async Task<int> CreateAsync(ClaimCreateViewModel vm, ClaimsPrincipal user)
    {
        var uid = Uid(user) ?? throw new InvalidOperationException("Bạn cần đăng nhập.");

        var item = await _db.FoundItem.AsNoTracking().FirstOrDefaultAsync(f => f.Id == vm.FoundItemId)
            ?? throw new InvalidOperationException("Không tìm thấy món đồ.");
        if ((FoundItemStatus)item.Status != FoundItemStatus.Open)
            throw new InvalidOperationException("Món đồ này hiện không thể gửi yêu cầu nhận lại.");
        if (uid == item.ReporterUserId || IsHolder(item, uid))
            throw new InvalidOperationException("Bạn không thể nhận lại món đồ do chính bạn giữ/đăng.");

        var hasActive = await _db.Claim.AsNoTracking().AnyAsync(c =>
            c.FoundItemId == vm.FoundItemId && c.ClaimantUserId == uid &&
            (c.Status == (int)ClaimStatus.Pending || c.Status == (int)ClaimStatus.Accepted));
        if (hasActive)
            throw new InvalidOperationException("Bạn đã có một yêu cầu đang xử lý cho món đồ này.");

        var files = vm.EvidenceImages ?? new List<IFormFile>();
        if (files.Count > MaxEvidence)
            throw new InvalidOperationException($"Tối đa {MaxEvidence} ảnh bằng chứng.");

        // Upload BEFORE the transaction (network I/O must not hold a tx open).
        var urls = new List<string>();
        foreach (var f in files)
        {
            var u = await _images.UploadAsync(f, EvidenceFolder);
            if (u is not null) urls.Add(u);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var claim = new LostAndFound.Models.Entities.Claim
        {
            FoundItemId = vm.FoundItemId,
            ClaimantUserId = uid,
            VerificationDetails = vm.VerificationDetails.Trim(),
            Status = (int)ClaimStatus.Pending
            // CreatedAt store-generated.
        };
        _db.Claim.Add(claim);
        await _db.SaveChangesAsync(); // get claim.Id

        for (int i = 0; i < urls.Count; i++)
            _db.ClaimImage.Add(new ClaimImage { ClaimId = claim.Id, Url = urls[i], SortOrder = i });
        if (urls.Count > 0) await _db.SaveChangesAsync();

        // Two rows, on purpose:
        // 1) the private audit-trail record, scoped to the Claim and naming the claimant.
        await _audit.LogAsync(uid, "ClaimSubmitted", "Claim", claim.Id.ToString(),
            null, ClaimStatus.Pending.ToString(), "Gửi yêu cầu nhận lại", isPublic: false);
        // 2) the public milestone on the ITEM timeline. Actor deliberately empty and no name in the
        //    detail, so the timeline shows THAT a claim arrived without outing WHO sent it (the "who"
        //    lives on Claim.ClaimantUserId, visible to the holder only). Same rule as the handover rows.
        await _audit.LogAsync("", "ClaimSubmitted", "FoundItem", item.Id.ToString(),
            null, null, "Có người gửi yêu cầu nhận lại", isPublic: true);

        // Notify the holder.
        var holderId = (HoldingType)item.HoldingType == HoldingType.SelfHeld ? item.ReporterUserId : item.CustodianStaffId;
        if (!string.IsNullOrEmpty(holderId))
            await _notify.PushAsync(holderId, "ClaimSubmitted", "Có yêu cầu nhận lại",
                $"Có người gửi yêu cầu nhận lại cho '{item.Title}'.", ItemLink(item.Id));

        await tx.CommitAsync();
        return claim.Id;
    }

    public async Task<bool> AcceptAsync(int claimId, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        if (uid is null) return false;

        var claim = await _db.Claim.FirstOrDefaultAsync(c => c.Id == claimId);
        if (claim is null || claim.Status != (int)ClaimStatus.Pending) return false;

        var item = await _db.FoundItem.FirstOrDefaultAsync(f => f.Id == claim.FoundItemId);
        if (item is null || (FoundItemStatus)item.Status != FoundItemStatus.Open) return false;
        if (!IsHolderOrAdmin(item, user)) return false;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        claim.Status = (int)ClaimStatus.Accepted;
        claim.HandledByUserId = uid;
        claim.HandledAt = now;

        item.Status = (int)FoundItemStatus.ClaimAccepted;
        item.HolderConfirmedHandover = false;
        item.ClaimantConfirmedHandover = false;

        // Auto-reject every OTHER pending claim on this item.
        var others = await _db.Claim
            .Where(c => c.FoundItemId == item.Id && c.Id != claim.Id && c.Status == (int)ClaimStatus.Pending)
            .ToListAsync();
        foreach (var o in others)
        {
            o.Status = (int)ClaimStatus.Rejected;
            o.HandledByUserId = uid;
            o.HandledAt = now;
            o.RejectReason = "Món đồ đã có người nhận.";
        }
        await _db.SaveChangesAsync();

        // Public item event (no names/verification).
        await _audit.LogAsync(uid, "ClaimAccepted", "FoundItem", item.Id.ToString(),
            FoundItemStatus.Open.ToString(), FoundItemStatus.ClaimAccepted.ToString(),
            "Đã chấp nhận một yêu cầu nhận lại", isPublic: true);

        // Notify the accepted claimant + each auto-rejected claimant.
        await _notify.PushAsync(claim.ClaimantUserId, "ClaimAccepted", "Yêu cầu nhận lại được chấp nhận",
            $"Yêu cầu nhận lại '{item.Title}' của bạn đã được chấp nhận. Hãy hẹn nhận đồ.", ItemLink(item.Id));
        foreach (var o in others)
            await _notify.PushAsync(o.ClaimantUserId, "ClaimRejected", "Yêu cầu nhận lại bị từ chối",
                $"Món đồ '{item.Title}' đã có người nhận.", ItemLink(item.Id));

        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> RejectAsync(int claimId, string rejectReason, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        if (uid is null) return false;
        if (string.IsNullOrWhiteSpace(rejectReason)) return false;

        var claim = await _db.Claim.FirstOrDefaultAsync(c => c.Id == claimId);
        if (claim is null || claim.Status != (int)ClaimStatus.Pending) return false;

        var item = await _db.FoundItem.FirstOrDefaultAsync(f => f.Id == claim.FoundItemId);
        if (item is null || !IsHolderOrAdmin(item, user)) return false;

        await using var tx = await _db.Database.BeginTransactionAsync();

        claim.Status = (int)ClaimStatus.Rejected;
        claim.HandledByUserId = uid;
        claim.HandledAt = DateTime.UtcNow;
        claim.RejectReason = rejectReason.Trim();
        await _db.SaveChangesAsync();

        // Private audit (has the reason).
        await _audit.LogAsync(uid, "ClaimRejected", "Claim", claim.Id.ToString(),
            ClaimStatus.Pending.ToString(), ClaimStatus.Rejected.ToString(), "Từ chối yêu cầu nhận lại", isPublic: false);

        await _notify.PushAsync(claim.ClaimantUserId, "ClaimRejected", "Yêu cầu nhận lại bị từ chối",
            $"Yêu cầu nhận lại '{item.Title}' bị từ chối. Lý do: {claim.RejectReason}", ItemLink(item.Id));

        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> ConfirmHandoverAsync(int foundItemId, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        if (uid is null) return false;

        var item = await _db.FoundItem.FirstOrDefaultAsync(f => f.Id == foundItemId);
        if (item is null || (FoundItemStatus)item.Status != FoundItemStatus.ClaimAccepted) return false;
        if (!IsHolderOrAdmin(item, user)) return false;

        item.HolderConfirmedHandover = true;
        return await FinalizeHandoverAsync(item, "Người giữ xác nhận đã bàn giao", holderActed: true);
    }

    public async Task<bool> ConfirmReceivedAsync(int foundItemId, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        if (uid is null) return false;

        var item = await _db.FoundItem.FirstOrDefaultAsync(f => f.Id == foundItemId);
        if (item is null || (FoundItemStatus)item.Status != FoundItemStatus.ClaimAccepted) return false;

        var accepted = await _db.Claim.FirstOrDefaultAsync(c =>
            c.FoundItemId == foundItemId && c.Status == (int)ClaimStatus.Accepted);
        if (accepted is null || accepted.ClaimantUserId != uid) return false; // only the accepted claimant

        item.ClaimantConfirmedHandover = true;
        return await FinalizeHandoverAsync(item, "Người nhận xác nhận đã nhận", holderActed: false);
    }

    /// <summary>Shared tail: write the confirmation audit, flip to Returned if both flags true, notify the
    /// counterpart. Assumes the caller has set the relevant flag on the tracked <paramref name="item"/>.</summary>
    private async Task<bool> FinalizeHandoverAsync(FoundItem item, string publicDetail, bool holderActed)
    {
        var accepted = await _db.Claim.FirstOrDefaultAsync(c =>
            c.FoundItemId == item.Id && c.Status == (int)ClaimStatus.Accepted);
        var holderId = (HoldingType)item.HoldingType == HoldingType.SelfHeld ? item.ReporterUserId : item.CustodianStaffId;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var from = FoundItemStatus.ClaimAccepted;

        // Persist THIS side's confirmation flag first (the caller already set it on the tracked item).
        await _db.SaveChangesAsync();

        // Re-read the authoritative flags from the DB. The FoundItem row lock serializes us against the
        // other party, so if BOTH confirmed — even near-simultaneously — exactly one transaction observes
        // both flags true here and flips the item to Returned. Deciding from the pre-transaction in-memory
        // copy (as before) let both sides miss it and stranded the item at ClaimAccepted forever.
        await _db.Entry(item).ReloadAsync();
        var bothConfirmed = item.HolderConfirmedHandover && item.ClaimantConfirmedHandover;
        if (bothConfirmed && (FoundItemStatus)item.Status == FoundItemStatus.ClaimAccepted)
        {
            item.Status = (int)FoundItemStatus.Returned;
            await _db.SaveChangesAsync();
        }

        // Public milestone row — actor intentionally OMITTED (empty) so the public timeline never names the
        // confirming party (especially the claimant, who would otherwise be outed as the reclaimer).
        // Accountability for accept/reject still lives on Claim.HandledByUserId.
        await _audit.LogAsync("", "HandoverConfirmed", "FoundItem", item.Id.ToString(),
            from.ToString(), ((FoundItemStatus)item.Status).ToString(), publicDetail, isPublic: true);

        // Notify the OTHER party about this confirmation.
        var counterpart = holderActed ? accepted?.ClaimantUserId : holderId;
        if (!string.IsNullOrEmpty(counterpart))
            await _notify.PushAsync(counterpart, "Handover", "Cập nhật bàn giao",
                $"{publicDetail} cho '{item.Title}'.", ItemLink(item.Id));

        if (bothConfirmed)
        {
            await _audit.LogAsync("", "Returned", "FoundItem", item.Id.ToString(),
                from.ToString(), FoundItemStatus.Returned.ToString(), "Đã trả đồ thành công", isPublic: true);
            if (!string.IsNullOrEmpty(holderId))
                await _notify.PushAsync(holderId, "Returned", "Đã hoàn tất trả đồ",
                    $"'{item.Title}' đã được trả thành công.", ItemLink(item.Id));
            if (!string.IsNullOrEmpty(accepted?.ClaimantUserId))
                await _notify.PushAsync(accepted!.ClaimantUserId, "Returned", "Đã hoàn tất trả đồ",
                    $"'{item.Title}' đã được trả thành công.", ItemLink(item.Id));
        }

        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> CancelAcceptanceAsync(int foundItemId, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        if (uid is null) return false;

        var item = await _db.FoundItem.FirstOrDefaultAsync(f => f.Id == foundItemId);
        if (item is null || (FoundItemStatus)item.Status != FoundItemStatus.ClaimAccepted) return false;
        if (!IsHolderOrAdmin(item, user)) return false;

        var accepted = await _db.Claim.FirstOrDefaultAsync(c =>
            c.FoundItemId == foundItemId && c.Status == (int)ClaimStatus.Accepted);

        await using var tx = await _db.Database.BeginTransactionAsync();

        item.Status = (int)FoundItemStatus.Open;
        item.HolderConfirmedHandover = false;
        item.ClaimantConfirmedHandover = false;
        if (accepted is not null)
        {
            accepted.Status = (int)ClaimStatus.Rejected;
            accepted.HandledByUserId = uid;
            accepted.HandledAt = DateTime.UtcNow;
            accepted.RejectReason = "Người giữ huỷ chấp nhận.";
        }
        await _db.SaveChangesAsync();

        await _audit.LogAsync(uid, "AcceptanceCancelled", "FoundItem", item.Id.ToString(),
            FoundItemStatus.ClaimAccepted.ToString(), FoundItemStatus.Open.ToString(),
            "Huỷ chấp nhận, mở lại yêu cầu", isPublic: true);

        if (accepted is not null)
            await _notify.PushAsync(accepted.ClaimantUserId, "AcceptanceCancelled", "Yêu cầu nhận lại đã bị huỷ",
                $"Người giữ đã huỷ chấp nhận yêu cầu nhận lại '{item.Title}'.", ItemLink(item.Id));

        await tx.CommitAsync();
        return true;
    }

    public async Task<ItemClaimPanelViewModel> GetItemClaimPanelAsync(int foundItemId, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        var item = await _db.FoundItem.AsNoTracking().FirstOrDefaultAsync(f => f.Id == foundItemId);
        if (item is null)
            return new ItemClaimPanelViewModel { FoundItemId = foundItemId };

        var status = (FoundItemStatus)item.Status;
        var isHolder = uid != null && IsHolderOrAdmin(item, user);

        // CanClaim: logged-in member, not holder/reporter, item Open, no active claim.
        bool canClaim = false;
        if (uid != null && status == FoundItemStatus.Open && uid != item.ReporterUserId && !IsHolder(item, uid))
        {
            canClaim = !await _db.Claim.AsNoTracking().AnyAsync(c =>
                c.FoundItemId == foundItemId && c.ClaimantUserId == uid &&
                (c.Status == (int)ClaimStatus.Pending || c.Status == (int)ClaimStatus.Accepted));
        }

        // Private claim data only for holder/admin (blind listing by construction).
        IReadOnlyList<ClaimForHolderViewModel> pending = Array.Empty<ClaimForHolderViewModel>();
        ClaimForHolderViewModel? accepted = null;
        if (isHolder)
        {
            pending = await BuildHolderClaimsAsync(foundItemId, ClaimStatus.Pending);
            accepted = (await BuildHolderClaimsAsync(foundItemId, ClaimStatus.Accepted)).FirstOrDefault();
        }

        // Accepted claimant (for handover buttons).
        var acceptedClaimantId = await _db.Claim.AsNoTracking()
            .Where(c => c.FoundItemId == foundItemId && c.Status == (int)ClaimStatus.Accepted)
            .Select(c => c.ClaimantUserId).FirstOrDefaultAsync();

        var isAcceptedClaimant = uid != null && uid == acceptedClaimantId;

        return new ItemClaimPanelViewModel
        {
            FoundItemId = foundItemId,
            Status = status,
            CanClaim = canClaim,
            IsHolderView = isHolder,
            PendingClaims = pending,
            AcceptedClaim = accepted,
            HolderConfirmed = item.HolderConfirmedHandover,
            ClaimantConfirmed = item.ClaimantConfirmedHandover,
            ShowHolderHandover = isHolder && status == FoundItemStatus.ClaimAccepted,
            ShowClaimantHandover = isAcceptedClaimant && status == FoundItemStatus.ClaimAccepted,
            CanCancelAcceptance = isHolder && status == FoundItemStatus.ClaimAccepted
        };
    }

    private async Task<List<ClaimForHolderViewModel>> BuildHolderClaimsAsync(int foundItemId, ClaimStatus status)
    {
        var rows = await _db.Claim.AsNoTracking()
            .Where(c => c.FoundItemId == foundItemId && c.Status == (int)status)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id, c.ClaimantUserId, c.CreatedAt, c.VerificationDetails,
                Images = c.ClaimImage.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList()
            })
            .ToListAsync();

        var result = new List<ClaimForHolderViewModel>();
        foreach (var r in rows)
        {
            result.Add(new ClaimForHolderViewModel
            {
                ClaimId = r.Id,
                ClaimantName = await NameOf(r.ClaimantUserId) ?? "N/A",
                CreatedAt = r.CreatedAt,
                VerificationDetails = r.VerificationDetails,
                EvidenceImagePaths = r.Images,
                Status = status
            });
        }
        return result;
    }

    public async Task<PagedResult<MyClaimViewModel>> GetMyClaimsAsync(string userId, ClaimStatus? status, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 50);

        var q = _db.Claim.AsNoTracking().Where(c => c.ClaimantUserId == userId);
        if (status is ClaimStatus s) q = q.Where(c => c.Status == (int)s);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new MyClaimViewModel
            {
                ClaimId = c.Id,
                FoundItemId = c.FoundItemId,
                ItemTitle = c.FoundItem.Title,
                ItemCoverImage = c.FoundItem.FoundItemImage.OrderBy(im => im.SortOrder).Select(im => im.Url).FirstOrDefault(),
                Status = (ClaimStatus)c.Status,
                ItemStatus = (FoundItemStatus)c.FoundItem.Status,
                RejectReason = c.RejectReason,
                CreatedAt = c.CreatedAt,
                HolderConfirmed = c.FoundItem.HolderConfirmedHandover,
                ClaimantConfirmed = c.FoundItem.ClaimantConfirmedHandover,
                CanConfirmReceived = c.Status == (int)ClaimStatus.Accepted
                    && c.FoundItem.Status == (int)FoundItemStatus.ClaimAccepted
                    && !c.FoundItem.ClaimantConfirmedHandover
            })
            .ToListAsync();

        return new PagedResult<MyClaimViewModel> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<ClaimDetailViewModel?> GetClaimDetailAsync(int claimId, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        if (uid is null) return null;

        var claim = await _db.Claim.AsNoTracking()
            .Include(c => c.ClaimImage)
            .FirstOrDefaultAsync(c => c.Id == claimId);
        if (claim is null) return null;

        var item = await _db.FoundItem.AsNoTracking().FirstOrDefaultAsync(f => f.Id == claim.FoundItemId);
        if (item is null) return null;

        // Only the claimant, the item's holder, or an Admin. (General staff arbitration = FR-CLAIM-06, deferred.)
        var isHolder = IsHolderOrAdmin(item, user);
        var isClaimant = claim.ClaimantUserId == uid;
        if (!isHolder && !isClaimant) return null;

        var status = (ClaimStatus)claim.Status;
        var itemStatus = (FoundItemStatus)item.Status;
        var live = status == ClaimStatus.Pending || status == ClaimStatus.Accepted;

        var msgs = await (
            from m in _db.ClaimMessage.AsNoTracking()
            join usr in _db.Users.AsNoTracking() on m.SenderUserId equals usr.Id into mu
            from usr in mu.DefaultIfEmpty()
            where m.ClaimId == claimId
            orderby m.CreatedAt
            select new ClaimMessageViewModel
            {
                Id = m.Id,
                SenderName = usr != null ? (usr.FullName ?? usr.Email!) : "N/A",
                Body = m.Body,
                CreatedAt = m.CreatedAt,
                IsMine = m.SenderUserId == uid
            }).ToListAsync();

        return new ClaimDetailViewModel
        {
            ClaimId = claim.Id,
            FoundItemId = item.Id,
            ItemTitle = item.Title,
            ItemCoverImage = await _db.FoundItemImage.AsNoTracking()
                .Where(i => i.FoundItemId == item.Id).OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefaultAsync(),
            ItemStatus = itemStatus,
            ClaimantName = await NameOf(claim.ClaimantUserId) ?? "N/A",
            Status = status,
            CreatedAt = claim.CreatedAt,
            RejectReason = claim.RejectReason,
            VerificationDetails = claim.VerificationDetails,
            EvidenceImagePaths = claim.ClaimImage.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
            Messages = msgs,
            ViewerIsHolder = isHolder,
            CanPostMessage = live,
            CanAccept = isHolder && status == ClaimStatus.Pending && itemStatus == FoundItemStatus.Open,
            CanReject = isHolder && status == ClaimStatus.Pending,
            CanConfirmReceived = isClaimant && status == ClaimStatus.Accepted
                                 && itemStatus == FoundItemStatus.ClaimAccepted && !item.ClaimantConfirmedHandover,
            HolderConfirmed = item.HolderConfirmedHandover,
            ClaimantConfirmed = item.ClaimantConfirmedHandover
        };
    }

    public async Task<bool> PostMessageAsync(int claimId, string body, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        if (uid is null || string.IsNullOrWhiteSpace(body)) return false;

        var claim = await _db.Claim.AsNoTracking().FirstOrDefaultAsync(c => c.Id == claimId);
        if (claim is null) return false;

        var status = (ClaimStatus)claim.Status;
        if (status == ClaimStatus.Rejected) return false; // closed claim -> thread is read-only

        var item = await _db.FoundItem.AsNoTracking().FirstOrDefaultAsync(f => f.Id == claim.FoundItemId);
        if (item is null) return false;

        var isHolder = IsHolderOrAdmin(item, user);
        var isClaimant = claim.ClaimantUserId == uid;
        if (!isHolder && !isClaimant) return false;

        var holderId = (HoldingType)item.HoldingType == HoldingType.SelfHeld ? item.ReporterUserId : item.CustodianStaffId;
        // The counterparty is the OTHER end of this thread.
        var recipient = isClaimant ? holderId : claim.ClaimantUserId;

        await using var tx = await _db.Database.BeginTransactionAsync();

        _db.ClaimMessage.Add(new ClaimMessage
        {
            ClaimId = claimId,
            SenderUserId = uid,
            Body = body.Trim()
            // CreatedAt is store-generated.
        });
        await _db.SaveChangesAsync();

        // No AuditLog: a message is not a status change, and the thread itself is the record.
        if (!string.IsNullOrEmpty(recipient))
            await _notify.PushAsync(recipient, "ClaimMessage", "Tin nhắn mới về yêu cầu nhận lại",
                $"Có tin nhắn mới về '{item.Title}'.", $"/Claims/Details/{claimId}");

        await tx.CommitAsync();
        return true;
    }
}
