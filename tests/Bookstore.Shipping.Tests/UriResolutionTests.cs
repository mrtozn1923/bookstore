namespace Bookstore.Shipping.Tests;

public sealed class UriResolutionTests
{
    [Theory]
    [InlineData("https://carrier.example.com/api/v2", "parcels/42", "https://carrier.example.com/api/parcels/42")]
    [InlineData("https://carrier.example.com/api/v2/", "parcels/42", "https://carrier.example.com/api/v2/parcels/42")]
    [InlineData("https://carrier.example.com/api/v2/", "/parcels/42", "https://carrier.example.com/parcels/42")]
    [InlineData("https://carrier.example.com/api/v2", "/parcels/42", "https://carrier.example.com/parcels/42")]
    public void Relative_uri_is_resolved_against_the_base_address(string baseAddress, string relative, string expected)
    {
        Uri resolved = new(new Uri(baseAddress), relative);

        Assert.Equal(expected, resolved.ToString());
    }
}
