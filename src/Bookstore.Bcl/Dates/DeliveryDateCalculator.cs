namespace Bookstore.Bcl.Dates;

// "Delivery date = 5 business days after approval", counted in Istanbul time.
// Orders approved at or after the cut-off start counting from the next day.
public sealed class DeliveryDateCalculator(TimeProvider clock, BusinessCalendar calendar, TimeZoneInfo zone)
{
    public static readonly TimeOnly CutOff = new(17, 0);

    public DeliveryQuote QuoteForApprovalNow(int businessDays = 5)
    {
        DateTimeOffset approvedAt = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone);
        DateOnly approvalDay = DateOnly.FromDateTime(approvedAt.DateTime);
        bool afterCutOff = TimeOnly.FromDateTime(approvedAt.DateTime) >= CutOff;

        DateOnly countFrom = afterCutOff ? approvalDay.AddDays(1) : approvalDay;
        DateOnly delivery = calendar.AddBusinessDays(countFrom, businessDays);
        return new DeliveryQuote(approvedAt, afterCutOff, delivery);
    }
}

public sealed record DeliveryQuote(DateTimeOffset ApprovedAt, bool AfterCutOff, DateOnly DeliveryDate);
