using BackendProjectTemplate.Infrastructure.Configuration;

namespace BackendProjectTemplate.WebAPI;

public static class ProductionSecrets
{
    public static IReadOnlyCollection<ProductionSecretRequirement> GetRequirements(IConfiguration configuration)
    {
        var requirements = new List<ProductionSecretRequirement>
        {
            ProductionSecretRequirement.Required("ConnectionStrings:PostgresWrite"),
            ProductionSecretRequirement.Required("ConnectionStrings:PostgresRead"),
            ProductionSecretRequirement.Required("ConnectionStrings:Redis"),
            ProductionSecretRequirement.JwtSigningKey(),
            ProductionSecretRequirement.Required("ObjectStorage:CloudflareR2:AccessKeyId"),
            ProductionSecretRequirement.Required("ObjectStorage:CloudflareR2:SecretAccessKey"),
            ProductionSecretRequirement.Required("Notifications:Email:Mailtrap:WebhookSigningSecret"),
            ProductionSecretRequirement.Required("IpGeolocation:IpInfo:AccessToken"),
            ProductionSecretRequirement.Required("IpGeolocation:IpWhoIs:ApiKey"),
            ProductionSecretRequirement.Required("Payments:Bachs:ApiKey"),
            ProductionSecretRequirement.Required("Payments:Bachs:WebhookSecret"),
            ProductionSecretRequirement.Required("Payments:Credo:SecretKey"),
            ProductionSecretRequirement.Required("Payments:SafeHaven:ClientId"),
            ProductionSecretRequirement.Required("Payments:SafeHaven:ClientAssertion")
        };

        if (configuration.GetValue<bool>("Kyc:Kycaid:Enabled"))
        {
            requirements.Add(ProductionSecretRequirement.Required("Kyc:Kycaid:ApiKey"));
        }

        return requirements;
    }
}
