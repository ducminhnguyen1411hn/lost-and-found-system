using Microsoft.AspNetCore.Identity;

namespace LostAndFound.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    public bool IsBlocked { get; set; }

    public bool IsPostingBlocked { get; set; }

}
