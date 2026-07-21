using LostAndFound.Data;
using LostAndFound.Models.Entities;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Camera;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

public class CameraService : ICameraService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public CameraService(ApplicationDbContext db, IAuditService auditService, INotificationService notificationService)
    {
        _db = db;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<CameraRequestCreateViewModel> GetCreateViewModelAsync()
        => new() { Locations = await BuildLocationsAsync() };

    public async Task<int> CreateAsync(CameraRequestCreateViewModel vm, string requesterUserId)
    {
        var req = new CameraCheckRequest
        {
            RequesterUserId = requesterUserId,
            LocationId = vm.LocationId!.Value,
            FromTime = AppTime.ToUtc(vm.FromTime!.Value),
            ToTime = AppTime.ToUtc(vm.ToTime!.Value),
            ItemDescription = vm.ItemDescription.Trim(),
            Status = (int)CameraRequestStatus.Pending
        };

        await using var tx = await _db.Database.BeginTransactionAsync();
        _db.CameraCheckRequest.Add(req);
        await _db.SaveChangesAsync();

        await _notificationService.PushToStaffAsync("CameraRequest",
            "Yêu cầu show camera mới",
            "Có người xin show camera — vào mục Yêu cầu camera để xử lý.",
            "/Camera");
        await tx.CommitAsync();
        return req.Id;
    }

    public async Task<List<CameraRequestViewModel>> GetMineAsync(string userId)
    {
        var rows = await _db.CameraCheckRequest
            .Where(r => r.RequesterUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new Row
            {
                Id = r.Id,
                LocationName = r.Location.Name,
                FromTime = r.FromTime,
                ToTime = r.ToTime,
                ItemDescription = r.ItemDescription,
                Status = r.Status,
                ResponseNote = r.ResponseNote,
                RequesterUserId = r.RequesterUserId,
                HandledByStaffId = r.HandledByStaffId,
                CreatedAt = r.CreatedAt,
                HandledAt = r.HandledAt
            })
            .ToListAsync();

        var names = await ResolveNamesAsync(rows);
        return rows.Select(r => Map(r, names)).ToList();
    }

    public async Task<CameraListViewModel> GetAllAsync(CameraRequestStatus? status)
    {
        var query = _db.CameraCheckRequest.AsQueryable();
        if (status.HasValue)
            query = query.Where(r => r.Status == (int)status.Value);

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new Row
            {
                Id = r.Id,
                LocationName = r.Location.Name,
                FromTime = r.FromTime,
                ToTime = r.ToTime,
                ItemDescription = r.ItemDescription,
                Status = r.Status,
                ResponseNote = r.ResponseNote,
                RequesterUserId = r.RequesterUserId,
                HandledByStaffId = r.HandledByStaffId,
                CreatedAt = r.CreatedAt,
                HandledAt = r.HandledAt
            })
            .ToListAsync();

        var names = await ResolveNamesAsync(rows);
        return new CameraListViewModel
        {
            Items = rows.Select(r => Map(r, names)).ToList(),
            StatusFilter = status
        };
    }

    public async Task<bool> RespondAsync(int id, CameraRequestStatus outcome, string? note, string staffUserId)
    {

        if (outcome != CameraRequestStatus.Resolved && outcome != CameraRequestStatus.Rejected) return false;

        var req = await _db.CameraCheckRequest.FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return false;
        if (req.Status is (int)CameraRequestStatus.Resolved or (int)CameraRequestStatus.Rejected) return false;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var from = (CameraRequestStatus)req.Status;
        req.Status = (int)outcome;
        req.ResponseNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        req.HandledByStaffId = staffUserId;
        req.HandledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(staffUserId, "CameraResponded", "CameraCheckRequest", id.ToString(),
            from.ToString(), outcome.ToString(), $"Phản hồi yêu cầu show camera: {StatusText((int)outcome)}", isPublic: false);

        await _notificationService.PushAsync(req.RequesterUserId, "CameraResponded",
            outcome == CameraRequestStatus.Resolved ? "Yêu cầu camera đã được xử lý" : "Yêu cầu camera bị từ chối",
            string.IsNullOrWhiteSpace(note) ? "Nhân viên đã phản hồi yêu cầu show camera của bạn." : $"Phản hồi: {note.Trim()}",
            "/Camera/Mine");

        await tx.CommitAsync();
        return true;
    }

    private async Task<List<SelectListItem>> BuildLocationsAsync()
    {
        var locs = await _db.Location.AsNoTracking().OrderBy(l => l.Building).ThenBy(l => l.Name).ToListAsync();
        return locs.Select(l => new SelectListItem
        {
            Value = l.Id.ToString(),
            Text = string.IsNullOrEmpty(l.Building) ? l.Name : $"{l.Building} - {l.Name}"
        }).ToList();
    }

    private async Task<Dictionary<string, string?>> ResolveNamesAsync(List<Row> rows)
    {
        var ids = rows.SelectMany(r => new[] { r.RequesterUserId, r.HandledByStaffId })
            .Where(id => id is not null).Select(id => id!).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, string?>();
        return await _db.Users.Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FullName ?? u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Name);
    }

    private static CameraRequestViewModel Map(Row r, Dictionary<string, string?> names) => new()
    {
        Id = r.Id,
        LocationName = r.LocationName,
        FromTime = r.FromTime,
        ToTime = r.ToTime,
        ItemDescription = r.ItemDescription,
        Status = (CameraRequestStatus)r.Status,
        StatusText = StatusText(r.Status),
        ResponseNote = r.ResponseNote,
        RequesterName = names.TryGetValue(r.RequesterUserId, out var rn) ? (rn ?? "?") : "?",
        HandledByName = r.HandledByStaffId is not null && names.TryGetValue(r.HandledByStaffId, out var hn) ? hn : null,
        CreatedAt = r.CreatedAt,
        HandledAt = r.HandledAt
    };

    private static string StatusText(int status) => (CameraRequestStatus)status switch
    {
        CameraRequestStatus.Pending => "Chờ xử lý",
        CameraRequestStatus.InReview => "Đang xem",
        CameraRequestStatus.Resolved => "Đã xử lý",
        CameraRequestStatus.Rejected => "Từ chối",
        _ => "?"
    };

    private sealed class Row
    {
        public int Id { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public DateTime FromTime { get; set; }
        public DateTime ToTime { get; set; }
        public string ItemDescription { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? ResponseNote { get; set; }
        public string RequesterUserId { get; set; } = string.Empty;
        public string? HandledByStaffId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? HandledAt { get; set; }
    }
}
