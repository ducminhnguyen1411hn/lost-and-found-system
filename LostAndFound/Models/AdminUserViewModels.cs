using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Models
{
    // Dùng cho trang danh sách Index
    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public IList<string> Roles { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsPostingBlocked { get; set; }
    }

    // Dùng cho trang chỉnh sửa Edit
    public class EditUserRolesViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsPostingBlocked { get; set; }
        public List<RoleSelectionViewModel> Roles { get; set; } = new List<RoleSelectionViewModel>();
    }

    // Đại diện cho từng dòng Checkbox Role
    public class RoleSelectionViewModel
    {
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}