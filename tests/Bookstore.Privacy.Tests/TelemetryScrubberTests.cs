using System.Net;
using Bookstore.Privacy.Telemetry;

namespace Bookstore.Privacy.Tests;

public sealed class TelemetryScrubberTests
{
    [Fact]
    public void Url_keeps_path_and_allowed_keys_only()
    {
        string url = "http://127.0.0.1:52806/orders/track?email=ada.testwell%40example.com&page=2#ref=newsletter";

        string scrubbed = TelemetryScrubber.ScrubUrl(url, new HashSet<string> { "page" });

        Assert.Equal("/orders/track?page=2", scrubbed);
    }

    [Fact]
    public void Text_loses_emails_and_long_numbers()
    {
        string message = "Invoice failed for ada.testwell@example.com, id 23515709358";

        Assert.Equal("Invoice failed for [email], id [number]", TelemetryScrubber.ScrubText(message));
    }

    [Fact]
    public void Ip_addresses_are_truncated()
    {
        Assert.Equal("127.0.0.0", TelemetryScrubber.TruncateIp(IPAddress.Loopback));
        Assert.Equal("127.0.0.0", TelemetryScrubber.TruncateIp(IPAddress.Loopback.MapToIPv6()));
        Assert.Equal("::", TelemetryScrubber.TruncateIp(IPAddress.IPv6Loopback));
    }
}
