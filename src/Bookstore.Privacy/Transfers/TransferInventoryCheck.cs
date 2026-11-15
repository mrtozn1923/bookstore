using Bookstore.Privacy.Consent;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Privacy.Transfers;

public static class TransferInventoryCheck
{
    // Law No. 6698 Art. 9(5): a signed standard contract is notified to the Authority within five business days.
    public const int NotificationBusinessDays = 5;

    // Every external service in configuration must be in the inventory, and every transfer abroad needs a valid mechanism.
    public static IReadOnlyList<string> Validate(IConfiguration externalServices, IEnumerable<SubProcessor> inventory, HolidayCalendar holidays)
    {
        Dictionary<string, SubProcessor> byKey = inventory.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        List<string> problems = [];

        foreach (IConfigurationSection service in externalServices.GetChildren())
        {
            if (!byKey.ContainsKey(service.Key))
            {
                problems.Add($"{service.Key}: configured ({service["Endpoint"]}) but not in the sub-processor inventory");
            }
        }

        foreach (SubProcessor s in byKey.Values.Where(s => s.Abroad))
        {
            switch (s.Mechanism)
            {
                case TransferMechanism.Domestic:
                    problems.Add($"{s.Key}: located abroad but marked as domestic");
                    break;
                case TransferMechanism.Occasional:
                    // Occasional transfers are irregular, one-off and outside the ordinary course of business.
                    problems.Add($"{s.Key}: a running service cannot rely on an occasional transfer");
                    break;
                case TransferMechanism.StandardContract when s.SignedOn is not { } signed:
                    problems.Add($"{s.Key}: standard contract without a signing date");
                    break;
                case TransferMechanism.StandardContract:
                    DateOnly due = BusinessDays.Add(s.SignedOn!.Value, NotificationBusinessDays, holidays.Days);
                    if (s.NotifiedOn is not { } notified || notified > due)
                    {
                        problems.Add($"{s.Key}: standard contract signed {s.SignedOn:yyyy-MM-dd}, notification due {due:yyyy-MM-dd}, notified {s.NotifiedOn?.ToString("yyyy-MM-dd") ?? "never"}");
                    }
                    break;
            }
        }
        return problems;
    }
}
