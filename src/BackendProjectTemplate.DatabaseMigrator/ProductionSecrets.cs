using BackendProjectTemplate.Infrastructure.Configuration;

namespace BackendProjectTemplate.DatabaseMigrator;

public static class ProductionSecrets
{
    public static IReadOnlyCollection<ProductionSecretRequirement> GetRequirements() =>
    [
        ProductionSecretRequirement.Required("ConnectionStrings:PostgresWrite")
    ];
}
