using System.ComponentModel.DataAnnotations;
using LostAndFound.Models.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LostAndFound.Models.ViewModels.LostItems;

/// <summary>Report-a-lost-item form. Public post; no PrivateMarks/HoldingType (the owner describes it).</summary>
public class LostItemCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(200)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khu vực nghi bị mất.")]
    [Display(Name = "Khu vực nghi bị mất")]
    public int? LocationId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời điểm mất.")]
    [NotInFuture(ErrorMessage = "Thời điểm mất không thể ở tương lai.")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-ddTHH:mm}")]
    [Display(Name = "Thời điểm mất")]
    public DateTime? LostAt { get; set; }

    [Display(Name = "Thẻ (phân tách bằng dấu phẩy)")]
    public string? TagsRaw { get; set; }

    [Display(Name = "Ảnh bìa (hiển thị ở danh sách)")]
    public IFormFile? CoverImage { get; set; }

    [Display(Name = "Ảnh khác (có thể chọn nhiều)")]
    public List<IFormFile>? OtherImages { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Locations { get; set; } = new List<SelectListItem>();

    public IEnumerable<string> TagList =>
        string.IsNullOrWhiteSpace(TagsRaw)
            ? Enumerable.Empty<string>()
            : TagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
