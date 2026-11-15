namespace Bookstore.Notifications.Client;

public static class ShippingNotifier
{
#if V2
    // 1.4.0: one more flag, with a default value
    public static string SendNotification(int orderId, bool viaSms, bool urgent, bool viaEmail = false) =>
        $"order {orderId}: inbox{(viaSms ? " + sms" : "")}{(viaEmail ? " + email" : "")}";
#else
    public static string SendNotification(int orderId, bool viaSms, bool urgent) =>
        $"order {orderId}: inbox{(viaSms ? " + sms" : "")}";
#endif
}
