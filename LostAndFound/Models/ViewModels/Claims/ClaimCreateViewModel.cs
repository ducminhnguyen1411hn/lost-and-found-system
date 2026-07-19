using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Models.ViewModels.Claims;

public class ClaimCreateViewModel
{
    public int FoundItemId { get; set; }

    // Read-only context (populated by the service for the GET form; not posted back for trust).
    public string ItemTitle { get; set; } = string.Empty;
    public string? ItemCoverImage { get; set; }

    [Required(ErrorMessage = "Hãy mô tả đặc điểm để chứng minh đây là đồ của bạn.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Mô tả từ 10 đến 2000 ký tự.")]
    [Display(Name = "Đặc điểm / bằng chứng sở hữu")]
    public string VerificationDetails { get; set; } = string.Empty;

    [Display(Name = "Ảnh bằng chứng (tối đa 5)")]
    public List<IFormFile>? EvidenceImages { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(30)]
    [Display(Name = "Số điện thoại (không bắt buộc)")]
    public string? ContactPhone { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256)]
    [Display(Name = "Email (không bắt buộc)")]
    public string? ContactEmail { get; set; }
}
