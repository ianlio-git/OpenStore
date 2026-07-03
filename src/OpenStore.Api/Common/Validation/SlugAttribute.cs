using System.ComponentModel.DataAnnotations;

namespace OpenStore.Api.Common.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SlugAttribute : ValidationAttribute
{
    private const int MaxLength = 100;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
        {
            return new ValidationResult("The slug field is required.");
        }

        if (value is not string slug)
        {
            return new ValidationResult("The slug field must be a string.");
        }

        string normalized = slug.Trim();

        if (normalized.Length > MaxLength)
        {
            return new ValidationResult($"The slug field must be a string with a maximum length of {MaxLength}.");
        }

        if (!normalized.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
        {
            return new ValidationResult("The slug field must contain only lowercase letters, digits, and hyphens.");
        }

        return ValidationResult.Success;
    }
}
