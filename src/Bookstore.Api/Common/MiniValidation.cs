using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Bookstore.Api.Common;

// Data annotations check for objects built in code (the merged PATCH result);
// request bodies are validated by AddValidation() before the handler runs.
public static class MiniValidation
{
    public static Dictionary<string, string[]> Validate(object instance)
    {
        Dictionary<string, string[]> errors = [];
        Collect(instance, prefix: "", errors);
        return errors;
    }

    private static void Collect(object instance, string prefix, Dictionary<string, string[]> errors)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        foreach (ValidationResult result in results)
        {
            foreach (string member in result.MemberNames)
            {
                errors[prefix + JsonNamingPolicy.CamelCase.ConvertName(member)] = [result.ErrorMessage ?? "Invalid value."];
            }
        }

        foreach (System.Reflection.PropertyInfo property in instance.GetType().GetProperties())
        {
            if (property.PropertyType.Namespace == instance.GetType().Namespace && property.GetValue(instance) is { } nested)
            {
                Collect(nested, prefix + JsonNamingPolicy.CamelCase.ConvertName(property.Name) + ".", errors);
            }
        }
    }
}
