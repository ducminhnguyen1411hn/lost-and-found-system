using System.ComponentModel.DataAnnotations;
using LostAndFound.Services;

namespace LostAndFound.Models.Validation;

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
