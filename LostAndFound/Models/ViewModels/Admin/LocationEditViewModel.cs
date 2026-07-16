using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Models.ViewModels.Admin;

public class LocationEditViewModel
{
    public int Id { get; set; }

    [StringLength(100, ErrorMessage = "Tên tòa nhà không được vượt quá 100 ký tự")]
    public string? Building { get; set; }

    [Required(ErrorMessage = "Tên địa điểm là bắt buộc")]
    [StringLength(150, ErrorMessage = "Tên địa điểm không được vượt quá 150 ký tự")]
    public string Name { get; set; } = string.Empty;
}