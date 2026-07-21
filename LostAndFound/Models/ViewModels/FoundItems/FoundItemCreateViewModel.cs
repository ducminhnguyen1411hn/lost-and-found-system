using System.ComponentModel.DataAnnotations;
using LostAndFound.Models.Enums;
using LostAndFound.Models.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LostAndFound.Models.ViewModels.FoundItems;

public class FoundItemCreateViewModel
{
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

    [Display(Name = "Hình thức giữ")]
    public HoldingType HoldingType { get; set; } = HoldingType.SelfHeld;

    [StringLength(1000)]
    [Display(Name = "Đặc điểm nhận dạng riêng (KHÔNG hiển thị công khai)")]
    public string? PrivateMarks { get; set; }

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
