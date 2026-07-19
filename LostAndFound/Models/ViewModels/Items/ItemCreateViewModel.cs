using System.ComponentModel.DataAnnotations;
using LostAndFound.Models.Enums;
using LostAndFound.Models.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LostAndFound.Models.ViewModels.Items;

/// <summary>The ONE create form. <see cref="Kind"/> picks which service handles the POST and which
/// fields show. HoldingType/PrivateMarks exist only on the found side (a lost post has no holder and
/// no secret marks to verify against).</summary>
public class ItemCreateViewModel
{
    [Display(Name = "Bạn muốn đăng gì?")]
    public ItemKind Kind { get; set; } = ItemKind.Found;

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

    [Required(ErrorMessage = "Vui lòng chọn địa điểm.")]
    [Display(Name = "Địa điểm")]
    public int? LocationId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời điểm.")]
    [NotInFuture(ErrorMessage = "Thời điểm không thể ở tương lai.")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-ddTHH:mm}")]
    [Display(Name = "Thời điểm")]
    public DateTime? OccurredAt { get; set; }

    [Display(Name = "Thẻ (phân tách bằng dấu phẩy)")]
    public string? TagsRaw { get; set; }

    [Display(Name = "Ảnh bìa")]
    public IFormFile? CoverImage { get; set; }

    [Display(Name = "Ảnh khác (có thể chọn nhiều)")]
    public List<IFormFile>? OtherImages { get; set; }

    // ---- found-side only ----
    [Display(Name = "Cách giữ")]
    public HoldingType HoldingType { get; set; } = HoldingType.SelfHeld;

    [StringLength(1000)]
    [Display(Name = "Đặc điểm nhận dạng riêng (KHÔNG hiển thị công khai)")]
    public string? PrivateMarks { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Locations { get; set; } = new List<SelectListItem>();
}
