namespace LostAndFound.Models.ViewModels.Admin;

public class UserRolesViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsBlocked { get; set; }
    public bool IsPostingBlocked { get; set; }
}

public class EditUserRolesViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public bool IsPostingBlocked { get; set; }
    public List<RoleSelectionViewModel> Roles { get; set; } = new List<RoleSelectionViewModel>();
}

public class RoleSelectionViewModel
{
    public string RoleName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
