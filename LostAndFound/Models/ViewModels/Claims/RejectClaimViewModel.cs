using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Models.ViewModels.Claims;

public class RejectClaimViewModel
{
    public int ClaimId { get; set; }

    [Required(ErrorMessage = "Hãy nêu lý do từ chối.")]
    [StringLength(1000, MinimumLength = 3)]
    [Display(Name = "Lý do từ chối")]
    public string RejectReason { get; set; } = string.Empty;
}
