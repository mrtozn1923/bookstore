using Bookstore.Api.Options;

namespace Bookstore.Api.Tests;

public class OptionsValidationTests
{
    [Fact]
    public void Jwt_signing_key_shorter_than_256_bits_fails()
    {
        var options = new JwtOptions { Issuer = "https://bookstore.example.com", SigningKey = "too-short" };

        var result = new JwtOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("SigningKey", result.FailureMessage);
    }

    [Fact]
    public void Empty_from_address_fails()
    {
        var options = new EmailOptions { FromAddress = "", ApiKey = "placeholder" };

        var result = new EmailOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Shipping_threshold_out_of_range_fails()
    {
        var options = new ShippingOptions { FreeShippingThreshold = -5, StandardFee = 49.90m };

        Assert.True(new ShippingOptionsValidator().Validate(null, options).Failed);
    }
}
