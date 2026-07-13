namespace LostAndFound.Services;

/// <summary>
/// Central time convention: the app stores ALL times in <b>UTC</b> (DB defaults use SYSUTCDATETIME;
/// FoundAt is converted on save) and displays them in the app's local zone (Vietnam, UTC+7).
/// Input forms work in local wall-clock — convert with <see cref="ToUtc"/> on save and
/// <see cref="ToLocal"/> when pre-filling / displaying.
/// </summary>
public static class AppTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        // Try IANA (Linux + Windows 10+/ICU) then the Windows id, then a fixed +7 fallback (VN has no DST).
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* try next */ }
        }
        return TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "Vietnam (UTC+7)", "VN");
    }

    /// <summary>Interpret a local wall-clock value (from a form) as app-local and convert to UTC.</summary>
    public static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    /// <summary>Convert a stored UTC value to app-local for display / pre-filling a form.</summary>
    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>App-local "now" (used for not-in-future checks and form defaults).</summary>
    public static DateTime LocalNow => ToLocal(DateTime.UtcNow);
}
