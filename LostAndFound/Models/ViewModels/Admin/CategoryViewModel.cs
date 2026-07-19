using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Models.ViewModels.Admin;

public class CategoryViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }

    public string? ParentName { get; set; }

    public bool HasChildren { get; set; }

    public int ItemCount { get; set; }

    public int Level { get; set; } // 0 = root, 1 = child, etc.
}