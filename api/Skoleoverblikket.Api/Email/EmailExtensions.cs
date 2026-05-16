namespace Skoleoverblikket.Api.Email;

public static class EmailExtensions
{
    public static IServiceCollection AddEmail(this IServiceCollection services)
    {
        services.AddOptions<SmtpOptions>()
            .BindConfiguration(SmtpOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<IEmailSender, MailKitEmailSender>();

        return services;
    }
}
