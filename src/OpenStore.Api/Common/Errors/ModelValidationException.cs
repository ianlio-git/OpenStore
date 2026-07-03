using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OpenStore.Api.Common.Errors;

public sealed class ModelValidationException : OpenStoreException
{
    private const string Code = "validation.failed";

    private const string DefaultMessage = "One or more validation errors occurred.";

    public ModelValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(Code, StatusCodes.Status400BadRequest, DefaultMessage)
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static ModelValidationException FromModelState(ModelStateDictionary modelState)
    {
        Dictionary<string, string[]> errors = [];

        foreach (KeyValuePair<string, ModelStateEntry> kvp in modelState)
        {
            if (kvp.Value.Errors.Count <= 0)
            {
                continue;
            }

            errors[ToCamelCase(kvp.Key)] = [.. kvp.Value.Errors.Select(e => e.ErrorMessage)];
        }

        ModelValidationException result = new(errors);

        return result;
    }

    public static ModelValidationException FromValidationResults(IEnumerable<ValidationResult> results)
    {
        Dictionary<string, string[]> errors = [];

        foreach (ValidationResult result in results)
        {
            if (result.ErrorMessage is null)
            {
                continue;
            }

            string[] memberNames = [.. result.MemberNames];

            if (memberNames.Length == 0)
            {
                memberNames = [""];
            }

            foreach (string memberName in memberNames)
            {
                string camelKey = ToCamelCase(memberName);

                if (errors.TryGetValue(camelKey, out string[]? existing))
                {
                    errors[camelKey] = [.. existing, result.ErrorMessage];
                }
                else
                {
                    errors[camelKey] = [result.ErrorMessage];
                }
            }
        }

        ModelValidationException resultException = new(errors);

        return resultException;
    }

    private static string ToCamelCase(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 2)
        {
            return key.ToLowerInvariant();
        }

        return char.ToLowerInvariant(key[0]) + key[1..];
    }
}
