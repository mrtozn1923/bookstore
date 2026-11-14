using System.ComponentModel.DataAnnotations;

namespace Bookstore.MinimalApi.Endpoints;

public static class ModelValidator
{
    public static Dictionary<string, string[]> Validate(object model)
    {
        List<ValidationResult> results = new();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        return results
            .SelectMany(r => r.MemberNames.Select(member => (Member: member, Message: r.ErrorMessage ?? "Invalid value.")))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());
    }
}
