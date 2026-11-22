using BackendProjectTemplate.Infrastructure.Configuration;

namespace BackendProjectTemplate.Jobs;

public static class ProductionSecrets
{
    public static IReadOnlyCollection<ProductionSecretRequirement> GetRequirements(IConfiguration configuration)
    {
        var requirements = new List<ProductionSecretRequirement>
        {
            ProductionSecretRequirement.Required("ConnectionStrings:PostgresWrite"),
            ProductionSecretRequirement.Required("ConnectionStrings:PostgresRead"),
            ProductionSecretRequirement.Required("ConnectionStrings:Redis"),
            ProductionSecretRequirement.Required("Messaging:RabbitMq:UserName"),
            ProductionSecretRequirement.Required("Messaging:RabbitMq:Password"),
            ProductionSecretRequirement.Required("IpGeolocation:IpInfo:AccessToken"),
            ProductionSecretRequirement.Required("IpGeolocation:IpWhoIs:ApiKey"),
            ProductionSecretRequirement.Required("Payments:Bachs:ApiKey"),
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
