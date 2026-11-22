using BackendProjectTemplate.Infrastructure.Configuration;
using Shouldly;
using System.Reflection;
using ConsumerProductionSecrets = BackendProjectTemplate.Consumer.ProductionSecrets;
using DatabaseMigratorProductionSecrets = BackendProjectTemplate.DatabaseMigrator.ProductionSecrets;
using JobsProductionSecrets = BackendProjectTemplate.Jobs.ProductionSecrets;
using WebApiProductionSecrets = BackendProjectTemplate.WebAPI.ProductionSecrets;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Configuration;

/// <summary>
/// Runs each real host entry point in Production to prove Program.cs wires the bootstrap in before the host is
/// built, so no request, background service or migration can run without delivered secrets.
/// </summary>
public sealed class When_StartingHostInProductionWithoutSecrets_Should
{
    public static TheoryData<string> Hosts => new()
    {
        "BackendProjectTemplate.WebAPI",
        "BackendProjectTemplate.Consumer",
        "BackendProjectTemplate.Jobs",
        "BackendProjectTemplate.DatabaseMigrator"
    };

    [Theory]
    [MemberData(nameof(Hosts))]
    public void FailBeforeTheHostIsBuilt(string applicationName)
    {
        Directory.Exists(ProductionConfigurationExtensions.DefaultSecretsDirectory).ShouldBeFalse(
            "This test proves startup fails without delivered secrets and would start a real host if " +
            $"'{ProductionConfigurationExtensions.DefaultSecretsDirectory}' existed on the test machine.");
        var entryPoint = GetHostAssembly(applicationName).EntryPoint.ShouldNotBeNull();

        var exception = Should.Throw<TargetInvocationException>(
            () => entryPoint.Invoke(null, [new[] { "--environment", "Production" }]));

        var failure = exception.InnerException.ShouldBeOfType<InvalidOperationException>();
        failure.Message.ShouldContain(applicationName);
        failure.Message.ShouldContain(ProductionConfigurationExtensions.DefaultSecretsDirectory);
        failure.Message.ShouldContain("does not exist");
    }

    private static Assembly GetHostAssembly(string applicationName) => applicationName switch
    {
        "BackendProjectTemplate.WebAPI" => typeof(WebApiProductionSecrets).Assembly,
        "BackendProjectTemplate.Consumer" => typeof(ConsumerProductionSecrets).Assembly,
        "BackendProjectTemplate.Jobs" => typeof(JobsProductionSecrets).Assembly,
        "BackendProjectTemplate.DatabaseMigrator" => typeof(DatabaseMigratorProductionSecrets).Assembly,
        _ => throw new ArgumentOutOfRangeException(nameof(applicationName), applicationName, null)
    };
}
