using Microsoft.Extensions.Configuration;
using Shouldly;
using ConsumerProductionSecrets = BackendProjectTemplate.Consumer.ProductionSecrets;
using DatabaseMigratorProductionSecrets = BackendProjectTemplate.DatabaseMigrator.ProductionSecrets;
using JobsProductionSecrets = BackendProjectTemplate.Jobs.ProductionSecrets;
using WebApiProductionSecrets = BackendProjectTemplate.WebAPI.ProductionSecrets;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Configuration;

public sealed class When_DeclaringHostProductionSecrets_Should
{
    [Fact]
    public void KeepDatabaseMigratorLimitedToPostgresWrite()
    {
        var keys = DatabaseMigratorProductionSecrets.GetRequirements()
            .Select(requirement => requirement.ConfigurationKey)
            .ToArray();

        keys.ShouldBe(["ConnectionStrings:PostgresWrite"]);
    }

    [Fact]
    public void KeepJobsIndependentOfObjectStorage()
    {
        var keys = JobsProductionSecrets.GetRequirements(CreateConfiguration())
            .Select(requirement => requirement.ConfigurationKey)
            .ToArray();

        keys.ShouldContain("ConnectionStrings:Redis");
        keys.ShouldContain("Messaging:RabbitMq:Password");
        keys.ShouldNotContain(key => key.StartsWith("ObjectStorage:", StringComparison.Ordinal));
    }

    [Fact]
    public void KeepWebApiAndConsumerRequirementsIsolated()
    {
        var webApiKeys = WebApiProductionSecrets.GetRequirements(CreateConfiguration())
            .Select(requirement => requirement.ConfigurationKey)
            .ToArray();
        var consumerKeys = ConsumerProductionSecrets.GetRequirements()
            .Select(requirement => requirement.ConfigurationKey)
            .ToArray();

        webApiKeys.ShouldContain("Jwt:SigningKey");
        webApiKeys.ShouldContain("Notifications:Email:Mailtrap:WebhookSigningSecret");
        webApiKeys.ShouldNotContain("Notifications:Email:Mailtrap:ApiToken");
        consumerKeys.ShouldContain("Notifications:Email:Mailtrap:ApiToken");
        consumerKeys.ShouldNotContain("Jwt:SigningKey");
        consumerKeys.ShouldNotContain("Notifications:Email:Mailtrap:WebhookSigningSecret");
    }

    [Fact]
    public void RequireKycaidApiKeyOnlyWhenKycaidIsEnabled()
    {
        var disabledKeys = WebApiProductionSecrets.GetRequirements(CreateConfiguration())
            .Select(requirement => requirement.ConfigurationKey)
            .ToArray();
        var enabledKeys = WebApiProductionSecrets.GetRequirements(CreateConfiguration(kycaidEnabled: true))
            .Select(requirement => requirement.ConfigurationKey)
            .ToArray();

        disabledKeys.ShouldNotContain("Kyc:Kycaid:ApiKey");
        enabledKeys.ShouldContain("Kyc:Kycaid:ApiKey");
    }

    private static IConfiguration CreateConfiguration(bool kycaidEnabled = false) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kyc:Kycaid:Enabled"] = kycaidEnabled.ToString()
            })
            .Build();
}
