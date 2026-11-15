using Bookstore.Notifications.Client;

Console.WriteLine(ShippingNotifier.SendNotification(42, viaSms: true, urgent: false));
