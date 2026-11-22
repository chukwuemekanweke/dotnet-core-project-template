using BackendProjectTemplate.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shouldly;
using ConsumerProductionSecrets = BackendProjectTemplate.Consumer.ProductionSecrets;
using DatabaseMigratorProductionSecrets = BackendProjectTemplate.DatabaseMigrator.ProductionSecrets;
using JobsProductionSecrets = BackendProjectTemplate.Jobs.ProductionSecrets;
using WebApiProductionSecrets = BackendProjectTemplate.WebAPI.ProductionSecrets;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Configuration;

public sealed class When_LoadingHostProductionSecrets_Should : IDisposable
{
    private const string SecretValue = "fake-secret-value-with-at-least-32-utf8-bytes";

    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"host-production-secrets-{Guid.CreateVersion7()}");

    public When_LoadingHostProductionSecrets_Should() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void StartEachHostWithOnlyItsOwnDeclaredSecrets()
    {
        AssertLoadsWithOnlyDeclaredSecrets("WebAPI", WebApiProductionSecrets.GetRequirements(CreateConfiguration()));
        AssertLoadsWithOnlyDeclaredSecrets("Consumer", ConsumerProductionSecrets.GetRequirements());
        AssertLoadsWithOnlyDeclaredSecrets("Jobs", JobsProductionSecrets.GetRequirements(CreateConfiguration()));
        AssertLoadsWithOnlyDeclaredSecrets("DatabaseMigrator", DatabaseMigratorProductionSecrets.GetRequirements());
    }

    [Fact]
    public void NotMakeAnotherServicesSecretsAvailableToWebApi()
    {
        WriteSecrets(ConsumerProductionSecrets.GetRequirements());

        var exception = Should.Throw<InvalidOperationException>(() => CreateProductionBuilder().AddProductionSecrets(
            "BackendProjectTemplate.WebAPI",
            WebApiProductionSecrets.GetRequirements(CreateConfiguration()),
            _directory));

        exception.Message.ShouldContain("BackendProjectTemplate.WebAPI");
        exception.Message.ShouldContain("Jwt:SigningKey");
        exception.Message.ShouldNotContain(SecretValue);
    }

    [Fact]
    public void FailWhenEnabledKycaidProviderHasNoApiKey()
    {
        WriteSecrets(WebApiProductionSecrets.GetRequirements(CreateConfiguration()));

        var exception = Should.Throw<InvalidOperationException>(() => CreateProductionBuilder().AddProductionSecrets(
            "BackendProjectTemplate.WebAPI",
            WebApiProductionSecrets.GetRequirements(CreateConfiguration(kycaidEnabled: true)),
            _directory));

        exception.Message.ShouldContain("Kyc:Kycaid:ApiKey");
        exception.Message.ShouldContain("missing");
    }

    private void AssertLoadsWithOnlyDeclaredSecrets(
        string service,
        IReadOnlyCollection<ProductionSecretRequirement> requirements)
    {
        foreach (var file in Directory.EnumerateFiles(_directory))
        {
            File.Delete(file);
        }

        WriteSecrets(requirements);
        var builder = CreateProductionBuilder();

        Should.NotThrow(
            () => builder.AddProductionSecrets($"BackendProjectTemplate.{service}", requirements, _directory),
            $"{service} should start with exactly its declared secret files.");

        foreach (var requirement in requirements)
        {
            builder.Configuration[requirement.ConfigurationKey].ShouldBe(SecretValue);
        }
    }

    private void WriteSecrets(IEnumerable<ProductionSecretRequirement> requirements)
    {
        foreach (var requirement in requirements)
        {
            File.WriteAllText(Path.Combine(_directory, requirement.FileName), SecretValue);
        }
    }

    private static WebApplicationBuilder CreateProductionBuilder() =>
        WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

    private static IConfiguration CreateConfiguration(bool kycaidEnabled = false) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kyc:Kycaid:Enabled"] = kycaidEnabled.ToString()
            })
            .Build();
}
