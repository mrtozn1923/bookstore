namespace Bookstore.Api;

public interface INotificationService
{
    string Send(int orderId);
}

public class EmailNotificationService : INotificationService
{
    public string Send(int orderId) => $"Email sent for order {orderId}";
}

public class SmsNotificationService : INotificationService
{
    public string Send(int orderId) => $"SMS sent for order {orderId}";
}
