using System.Text;

namespace Bookstore.CryptoLab;

// Deliberately fake, readable keys so that every reader gets the same output. Never use in production.
public static class DevKeys
{
    public static byte[] MasterKey => Encoding.ASCII.GetBytes("dev-only-bookstore-master-key-32");
    public static byte[] PaymentWebhookSecret => Encoding.ASCII.GetBytes("dev-only-payment-webhook-secret-v2");
    public static byte[] PreviousPaymentWebhookSecret => Encoding.ASCII.GetBytes("dev-only-payment-webhook-secret-v1");
}
