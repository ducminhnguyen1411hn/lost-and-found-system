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
        if ((FoundItemStatus)item.Status != FoundItemStatus.Open) return null;
        if (uid == item.ReporterUserId || IsHolder(item, uid)) return null;

        var hasActive = await _db.Claim.AsNoTracking().AnyAsync(c =>
            c.FoundItemId == foundItemId && c.ClaimantUserId == uid &&
            (c.Status == (int)ClaimStatus.Pending || c.Status == (int)ClaimStatus.Accepted));
        if (hasActive) return null;

        var profile = await _db.Users.AsNoTracking()
            .Where(u => u.Id == uid).Select(u => new { u.PhoneNumber, u.Email }).FirstOrDefaultAsync();

        return new ClaimCreateViewModel
        {
            FoundItemId = item.Id,
            ItemTitle = item.Title,
            ItemCoverImage = item.FoundItemImage.OrderBy(im => im.SortOrder).Select(im => im.Url).FirstOrDefault(),
            ContactPhone = profile?.PhoneNumber,
            ContactEmail = profile?.Email
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
            ContactPhone = string.IsNullOrWhiteSpace(vm.ContactPhone) ? null : vm.ContactPhone.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(vm.ContactEmail) ? null : vm.ContactEmail.Trim(),
            Status = (int)ClaimStatus.Pending

        };
        _db.Claim.Add(claim);
        await _db.SaveChangesAsync();

        for (int i = 0; i < urls.Count; i++)
            _db.ClaimImage.Add(new ClaimImage { ClaimId = claim.Id, Url = urls[i], SortOrder = i });
        if (urls.Count > 0) await _db.SaveChangesAsync();

        await _audit.LogAsync(uid, "ClaimSubmitted", "Claim", claim.Id.ToString(),
            null, ClaimStatus.Pending.ToString(), "Gửi yêu cầu nhận lại", isPublic: false);

        await _audit.LogAsync("", "ClaimSubmitted", "FoundItem", item.Id.ToString(),
            null, null, "Có người gửi yêu cầu nhận lại", isPublic: true);

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
        item.HolderConfirmedAt = null;
        item.ClaimantConfirmedAt = null;

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

        await _audit.LogAsync(uid, "ClaimAccepted", "FoundItem", item.Id.ToString(),
            FoundItemStatus.Open.ToString(), FoundItemStatus.ClaimAccepted.ToString(),
            "Đã chấp nhận một yêu cầu nhận lại", isPublic: true);

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
        item.HolderConfirmedAt = DateTime.UtcNow;
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
        if (accepted is null || accepted.ClaimantUserId != uid) return false;

        item.ClaimantConfirmedHandover = true;
        item.ClaimantConfirmedAt = DateTime.UtcNow;
        return await FinalizeHandoverAsync(item, "Người nhận xác nhận đã nhận", holderActed: false);
    }

    private async Task<bool> FinalizeHandoverAsync(FoundItem item, string publicDetail, bool holderActed)
    {
        var accepted = await _db.Claim.FirstOrDefaultAsync(c =>
            c.FoundItemId == item.Id && c.Status == (int)ClaimStatus.Accepted);
        var holderId = (HoldingType)item.HoldingType == HoldingType.SelfHeld ? item.ReporterUserId : item.CustodianStaffId;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var from = FoundItemStatus.ClaimAccepted;

        await _db.SaveChangesAsync();

        await _db.Entry(item).ReloadAsync();
        var bothConfirmed = item.HolderConfirmedHandover && item.ClaimantConfirmedHandover;
        if (bothConfirmed && (FoundItemStatus)item.Status == FoundItemStatus.ClaimAccepted)
        {
            item.Status = (int)FoundItemStatus.Returned;
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync("", "HandoverConfirmed", "FoundItem", item.Id.ToString(),
            from.ToString(), ((FoundItemStatus)item.Status).ToString(), publicDetail, isPublic: true);

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
        item.HolderConfirmedAt = null;
        item.ClaimantConfirmedAt = null;
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

        bool canClaim = false;
        if (uid != null && status == FoundItemStatus.Open && uid != item.ReporterUserId && !IsHolder(item, uid))
        {
            canClaim = !await _db.Claim.AsNoTracking().AnyAsync(c =>
                c.FoundItemId == foundItemId && c.ClaimantUserId == uid &&
                (c.Status == (int)ClaimStatus.Pending || c.Status == (int)ClaimStatus.Accepted));
        }

        var acceptedClaimantId = await _db.Claim.AsNoTracking()
            .Where(c => c.FoundItemId == foundItemId && c.Status == (int)ClaimStatus.Accepted)
            .Select(c => c.ClaimantUserId).FirstOrDefaultAsync();
        var isAcceptedClaimant = uid != null && uid == acceptedClaimantId;

        IReadOnlyList<ClaimForHolderViewModel> pending = Array.Empty<ClaimForHolderViewModel>();
        IReadOnlyList<ClaimForHolderViewModel> rejected = Array.Empty<ClaimForHolderViewModel>();
        ClaimForHolderViewModel? accepted = null;
        if (isHolder)
        {
            pending = await BuildHolderClaimsAsync(foundItemId, ClaimStatus.Pending);
            accepted = (await BuildHolderClaimsAsync(foundItemId, ClaimStatus.Accepted)).FirstOrDefault();
            rejected = await BuildHolderClaimsAsync(foundItemId, ClaimStatus.Rejected);
        }
        else if (isAcceptedClaimant)
        {
            accepted = (await BuildHolderClaimsAsync(foundItemId, ClaimStatus.Accepted)).FirstOrDefault();
        }

        var handover = await BuildHandoverAsync(item, acceptedClaimantId, isHolder, isAcceptedClaimant);

        return new ItemClaimPanelViewModel
        {
            FoundItemId = foundItemId,
            Status = status,
            CanClaim = canClaim,
            IsHolderView = isHolder,
            PendingClaims = pending,
            AcceptedClaim = accepted,
            RejectedClaims = rejected,
            ViewerIsAcceptedClaimant = isAcceptedClaimant,
            Handover = handover
        };
    }

    private async Task<HandoverPanelViewModel?> BuildHandoverAsync(
    FoundItem item, string? acceptedClaimantId, bool viewerIsHolder, bool viewerIsClaimant)
    {
        if ((FoundItemStatus)item.Status != FoundItemStatus.ClaimAccepted) return null;
        if (string.IsNullOrEmpty(acceptedClaimantId)) return null;
        if (!viewerIsHolder && !viewerIsClaimant) return null;

        var holderId = (HoldingType)item.HoldingType == HoldingType.SelfHeld ? item.ReporterUserId : item.CustodianStaffId;

        return new HandoverPanelViewModel
        {
            FoundItemId = item.Id,
            HolderName = string.IsNullOrEmpty(holderId) ? "N/A" : (await NameOf(holderId) ?? "N/A"),
            ClaimantName = await NameOf(acceptedClaimantId) ?? "N/A",
            HolderConfirmed = item.HolderConfirmedHandover,
            ClaimantConfirmed = item.ClaimantConfirmedHandover,
            HolderConfirmedAt = item.HolderConfirmedAt,
            ClaimantConfirmedAt = item.ClaimantConfirmedAt,
            ViewerIsHolder = viewerIsHolder,
            ViewerIsClaimant = viewerIsClaimant,
            CanCancelAcceptance = viewerIsHolder
        };
    }

    private async Task<List<ClaimForHolderViewModel>> BuildHolderClaimsAsync(int foundItemId, ClaimStatus status)
    {
        var rows = await _db.Claim.AsNoTracking()
            .Where(c => c.FoundItemId == foundItemId && c.Status == (int)status)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.ClaimantUserId,
                c.CreatedAt,
                c.VerificationDetails,
                c.ContactPhone,
                c.ContactEmail,
                c.RejectReason,
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
                Status = status,
                ContactPhone = r.ContactPhone,
                ContactEmail = r.ContactEmail,
                RejectReason = r.RejectReason
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
            ContactPhone = claim.ContactPhone,
            ContactEmail = claim.ContactEmail,
            Messages = msgs,
            ViewerIsHolder = isHolder,
            CanPostMessage = live,
            CanAccept = isHolder && status == ClaimStatus.Pending && itemStatus == FoundItemStatus.Open,
            CanReject = isHolder && status == ClaimStatus.Pending,

            Handover = await BuildHandoverAsync(
                item,
                status == ClaimStatus.Accepted ? claim.ClaimantUserId : null,
                isHolder,
                isClaimant && status == ClaimStatus.Accepted)
        };
    }

    public async Task<bool> PostMessageAsync(int claimId, string body, ClaimsPrincipal user)
    {
        var uid = Uid(user);
        if (uid is null || string.IsNullOrWhiteSpace(body)) return false;

        var claim = await _db.Claim.AsNoTracking().FirstOrDefaultAsync(c => c.Id == claimId);
        if (claim is null) return false;

        var status = (ClaimStatus)claim.Status;
        if (status == ClaimStatus.Rejected) return false;

        var item = await _db.FoundItem.AsNoTracking().FirstOrDefaultAsync(f => f.Id == claim.FoundItemId);
        if (item is null) return false;

        var isHolder = IsHolderOrAdmin(item, user);
        var isClaimant = claim.ClaimantUserId == uid;
        if (!isHolder && !isClaimant) return false;

        var holderId = (HoldingType)item.HoldingType == HoldingType.SelfHeld ? item.ReporterUserId : item.CustodianStaffId;

        var recipient = isClaimant ? holderId : claim.ClaimantUserId;

        await using var tx = await _db.Database.BeginTransactionAsync();

        _db.ClaimMessage.Add(new ClaimMessage
        {
            ClaimId = claimId,
            SenderUserId = uid,
            Body = body.Trim()

        });
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(recipient))
            await _notify.PushAsync(recipient, "ClaimMessage", "Tin nhắn mới về yêu cầu nhận lại",
                $"Có tin nhắn mới về '{item.Title}'.", $"/Claims/Details/{claimId}");

        await tx.CommitAsync();
        return true;
    }
}
