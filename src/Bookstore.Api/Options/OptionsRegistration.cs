using Microsoft.Extensions.Options;

namespace Bookstore.Api.Options;

public static class OptionsRegistration
{
    public static IServiceCollection AddBookstoreOptions(this IServiceCollection services)
    {
        services.AddOptions<ShippingOptions>().BindConfiguration(ShippingOptions.SectionName).ValidateOnStart();
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName).ValidateOnStart();
        services.AddOptions<EmailOptions>().BindConfiguration(EmailOptions.SectionName).ValidateOnStart();
        services.AddOptions<PaymentWebhookOptions>().BindConfiguration(PaymentWebhookOptions.SectionName);

        // Reflection yerine source generator'ın ürettiği validator'lar
        services.AddSingleton<IValidateOptions<ShippingOptions>, ShippingOptionsValidator>();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        return services;
    }
}
