using Microsoft.AspNetCore.Identity;

namespace LostAndFound.Models;

/// <summary>
/// The application user. Extends ASP.NET Core Identity with the project's profile fields.
/// HAND-WRITTEN — lives outside <c>Models/Entities</c> so a DB-First re-scaffold never
/// overwrites it. The matching columns live on the <c>AspNetUsers</c> table (see db/schema.sql).
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Full display name. Nullable at the data layer so the default Identity
    /// register page still works; the FR-AUTH feature makes it required at the app layer.</summary>
    public string? FullName { get; set; }

    /// <summary>Whether the user is completely blocked from accessing the system.</summary>
    public bool IsBlocked { get; set; }

    /// <summary>Whether the user is blocked from posting found items (but can still browse).</summary>
    public bool IsPostingBlocked { get; set; }

}
