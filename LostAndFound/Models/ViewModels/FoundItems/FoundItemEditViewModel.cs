using System.ComponentModel.DataAnnotations;
using LostAndFound.Models.Enums;
using LostAndFound.Models.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LostAndFound.Models.ViewModels.FoundItems;

/// <summary>Edit-a-found-item form. Owner-only; HoldingType is shown read-only (changing it would
/// alter the state machine/holder). Uploading a new image replaces the old one; leaving it empty keeps it.</summary>
public class FoundItemEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(200)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Mô tả (hiển thị công khai)")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nơi nhặt được.")]
    [Display(Name = "Nơi nhặt được")]
    public int? LocationId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời điểm nhặt được.")]
    [NotInFuture(ErrorMessage = "Thời điểm nhặt được không thể ở tương lai.")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-ddTHH:mm}")]
    [Display(Name = "Thời điểm nhặt được")]
    public DateTime? FoundAt { get; set; }

    [StringLength(1000)]
    [Display(Name = "Đặc điểm nhận dạng riêng (KHÔNG hiển thị công khai)")]
    public string? PrivateMarks { get; set; }

    [Display(Name = "Thẻ (phân tách bằng dấu phẩy)")]
    public string? TagsRaw { get; set; }

    [Display(Name = "Thêm ảnh mới (có thể chọn nhiều)")]
    public List<IFormFile>? NewImages { get; set; }

    /// <summary>Ids of existing images the user ticked to remove.</summary>
    public List<int> RemoveImageIds { get; set; } = new();

    /// <summary>Existing-image Id the user picked as the cover (gets SortOrder 0). Defaults to the current cover.</summary>
    public int? CoverImageId { get; set; }

    /// <summary>Existing images shown for management (cover first).</summary>
    public IReadOnlyList<ImageItem> ExistingImages { get; set; } = Array.Empty<ImageItem>();

    public HoldingType HoldingType { get; set; } // read-only display

    public class ImageItem
    {
        public int Id { get; init; }
        public string Url { get; init; } = string.Empty;
    }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Locations { get; set; } = new List<SelectListItem>();

    public IEnumerable<string> TagList =>
        string.IsNullOrWhiteSpace(TagsRaw)
            ? Enumerable.Empty<string>()
            : TagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
