namespace LostAndFound.Models.ViewModels.Admin;

// Dùng cho trang danh sách Index
public class UserRolesViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsBlocked { get; set; }
    public bool IsPostingBlocked { get; set; }
}

// Dùng cho trang chỉnh sửa Edit
public class EditUserRolesViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public bool IsPostingBlocked { get; set; }
    public List<RoleSelectionViewModel> Roles { get; set; } = new List<RoleSelectionViewModel>();
}

// Đại diện cho từng dòng Checkbox Role
public class RoleSelectionViewModel
{
    public string RoleName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
