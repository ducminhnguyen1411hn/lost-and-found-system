namespace LostAndFound.Services;

public static class AppTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {

        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "Vietnam (UTC+7)", "VN");
    }

    public static DateTime ToUtc(DateTime local) =>
    TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    public static DateTime ToLocal(DateTime utc) =>
    TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateTime LocalNow => ToLocal(DateTime.UtcNow);

    public static string Relative(DateTime utc)
    {
        var span = LocalNow - ToLocal(utc);
        if (span.TotalMinutes < 1) return "vừa xong";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
        if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)} tuần trước";
        return ToLocal(utc).ToString("dd/MM/yyyy");
    }
}
