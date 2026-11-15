namespace Bookstore.Admin.Validation;

public static class ValidationServiceExtensions
{
    // The validation source generator emits metadata for the assembly that calls AddValidation().
    // Calling it here, inside Bookstore.Admin, lets Program.cs and the bUnit tests share the same metadata.
    public static IServiceCollection AddAdminValidation(this IServiceCollection services) => services.AddValidation();
}
