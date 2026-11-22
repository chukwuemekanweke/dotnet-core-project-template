using BackendProjectTemplate.Infrastructure.Configuration;

namespace BackendProjectTemplate.Consumer;

public static class ProductionSecrets
{
    public static IReadOnlyCollection<ProductionSecretRequirement> GetRequirements() =>
    [
        ProductionSecretRequirement.Required("ConnectionStrings:PostgresWrite"),
        ProductionSecretRequirement.Required("ConnectionStrings:PostgresRead"),
        ProductionSecretRequirement.Required("ConnectionStrings:Redis"),
        ProductionSecretRequirement.Required("Messaging:RabbitMq:UserName"),
        ProductionSecretRequirement.Required("Messaging:RabbitMq:Password"),
        ProductionSecretRequirement.Required("ObjectStorage:CloudflareR2:AccessKeyId"),
        ProductionSecretRequirement.Required("ObjectStorage:CloudflareR2:SecretAccessKey"),
        ProductionSecretRequirement.Required("Notifications:Email:Mailtrap:ApiToken"),
        ProductionSecretRequirement.Required("Payments:Bachs:ApiKey")
    ];
}
