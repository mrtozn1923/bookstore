using System.ComponentModel.DataAnnotations;
using Bookstore.Api.Localization;
using Microsoft.Extensions.Localization;

namespace Bookstore.Api.Institutional;

public record InstitutionOrderLine(
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.Required))]
    string Isbn,
    [Range(1, 500, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.Range))]
    int Quantity);

public record InstitutionOrderRequest(List<InstitutionOrderLine> Items) : IValidatableObject
{
    public const int MinimumQuantity = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        int total = Items.Sum(i => i.Quantity);
        if (total < MinimumQuantity)
        {
            IStringLocalizer<SharedMessages> localizer =
                validationContext.GetRequiredService<IStringLocalizer<SharedMessages>>();
            yield return new ValidationResult(
                localizer["MinimumInstitutionalQuantity", MinimumQuantity, total],
                [nameof(Items)]);
        }
    }
}
