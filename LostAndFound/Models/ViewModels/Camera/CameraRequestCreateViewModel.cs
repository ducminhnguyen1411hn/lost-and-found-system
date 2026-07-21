using System.ComponentModel.DataAnnotations;
using LostAndFound.Models.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LostAndFound.Models.ViewModels.Camera;

public class CameraRequestCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn khu vực.")]
    [Display(Name = "Khu vực")]
    public int? LocationId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời điểm bắt đầu.")]
    [NotInFuture(ErrorMessage = "Thời điểm không thể ở tương lai.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Từ lúc")]
    public DateTime? FromTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời điểm kết thúc.")]
    [NotInFuture(ErrorMessage = "Thời điểm không thể ở tương lai.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Đến lúc")]
    public DateTime? ToTime { get; set; }

    [Required(ErrorMessage = "Vui lòng mô tả món đồ cần tìm.")]
    [StringLength(1000)]
    [Display(Name = "Mô tả món đồ")]
    public string ItemDescription { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> Locations { get; set; } = new List<SelectListItem>();
}
