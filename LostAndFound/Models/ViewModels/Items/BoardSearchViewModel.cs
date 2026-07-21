using System.ComponentModel.DataAnnotations;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LostAndFound.Models.ViewModels.Items;

public class BoardSearchViewModel : IValidatableObject
{
    public ItemKind? Kind { get; set; }
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public int? LocationId { get; set; }
    public string? Tag { get; set; }

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? From { get; set; }

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? To { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Locations { get; set; } = new List<SelectListItem>();
    public PagedResult<BoardItemViewModel> Results { get; set; } = new();

    public int ActiveFilterCount =>
    (Kind.HasValue ? 1 : 0) +
    (string.IsNullOrWhiteSpace(Keyword) ? 0 : 1) +
    (CategoryId.HasValue ? 1 : 0) +
    (LocationId.HasValue ? 1 : 0) +
    (string.IsNullOrWhiteSpace(Tag) ? 0 : 1) +
    (From.HasValue ? 1 : 0) + (To.HasValue ? 1 : 0);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From.Value > To.Value)
            yield return new ValidationResult("\"Từ\" phải trước hoặc bằng \"Đến\".", new[] { nameof(From), nameof(To) });
    }
}
