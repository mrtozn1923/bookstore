namespace Bookstore.Api.Services;

public interface INotifier
{
    string Name { get; }
}

public class EmailNotifier : INotifier
{
    public string Name => "email";
}

public class SmsNotifier : INotifier
{
    public string Name => "sms";
}
