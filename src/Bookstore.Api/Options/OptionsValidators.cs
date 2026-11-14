using Microsoft.Extensions.Options;

namespace Bookstore.Api.Options;

[OptionsValidator]
public sealed partial class ShippingOptionsValidator : IValidateOptions<ShippingOptions>;

[OptionsValidator]
public sealed partial class JwtOptionsValidator : IValidateOptions<JwtOptions>;

[OptionsValidator]
public sealed partial class EmailOptionsValidator : IValidateOptions<EmailOptions>;
