using System.ComponentModel.DataAnnotations;
using LostAndFound.Services;

namespace LostAndFound.Models.Validation;

/// <summary>Validation: a <see cref="DateTime"/> must not be in the future (NFR-05, FoundAt).
/// The bound value is app-local wall-clock, so compare against app-local now (not server-local).
/// Null passes — pair with <c>[Required]</c> when the value is mandatory.</summary>
public class NotInFutureAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null) return ValidationResult.Success;
        if (value is DateTime dt && dt > AppTime.LocalNow)
        {
            return new ValidationResult(
                ErrorMessage ?? "Giá trị không được ở tương lai.",
                validationContext.MemberName is null ? null : new[] { validationContext.MemberName });
        }
        return ValidationResult.Success;
    }
}
