using System.ComponentModel.DataAnnotations;

namespace OpenStore.Api.Common.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiredGuidAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is Guid guid && guid == Guid.Empty)
        {
            return new ValidationResult("The field is required.");
        }

        return ValidationResult.Success;
    }
}
