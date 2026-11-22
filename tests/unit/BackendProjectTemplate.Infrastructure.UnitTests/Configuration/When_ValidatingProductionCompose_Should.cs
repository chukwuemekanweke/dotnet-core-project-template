using Shouldly;
using System.Text.RegularExpressions;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Configuration;

public sealed class When_ValidatingProductionCompose_Should
{
    [Fact]
    public void MountIsolatedSecretsReadOnlyWithoutCreatingHostPaths()
    {
        var compose = ReadProductionCompose();

        foreach (var service in new[] { "webapi", "consumer", "jobs", "database-migrator" })
        {
            compose.ShouldContain($"/{service}/current");
        }

        Regex.Matches(compose, "target: /run/secrets/app").Count.ShouldBe(4);
        Regex.Matches(compose, "read_only: true").Count.ShouldBe(4);
        Regex.Matches(compose, "create_host_path: false").Count.ShouldBe(4);
        // The web/worker hosts read ASPNETCORE_ENVIRONMENT; the one-shot migrator is a Generic Host (DOTNET_ENVIRONMENT).
        Regex.Matches(compose, "(ASPNETCORE|DOTNET)_ENVIRONMENT: Production").Count.ShouldBe(4);
    }

    [Fact]
    public void RunTheMigratorOnlyAsAnExplicitOneShotService()
    {
        var migrator = ReadServiceBlock("database-migrator");

        migrator.ShouldContain("profiles: [\"migration\"]");
        migrator.ShouldContain("restart: \"no\"");
        migrator.ShouldContain("no-new-privileges:true");
        migrator.ShouldNotContain("ports:");
        migrator.ShouldNotContain("healthcheck:");
        migrator.ShouldNotContain("image:");

        foreach (var service in new[] { "webapi", "consumer", "jobs" })
        {
            ReadServiceBlock(service).ShouldNotContain("database-migrator", customMessage:
                $"Long-running '{service}' must not depend on the migrator; the deployment orchestrator runs it first.");
        }
    }

    [Fact]
    public void WaitForSuccessfulMigratorCompletionInLocalCompose()
    {
        var compose = ReadComposeFile("docker-compose.yml").ReplaceLineEndings("\n");
        var migrator = Regex.Match(compose, @"^  database-migrator:\n(?<body>(?:(?:    .*)?\n)*)", RegexOptions.Multiline)
            .Groups["body"].Value;

        migrator.ShouldNotContain("ports:");
        migrator.ShouldNotContain("healthcheck:");
        Regex.Matches(compose, @"database-migrator:\n\s+condition: service_completed_successfully").Count.ShouldBe(3);
        Regex.IsMatch(compose, @"database-migrator:\n\s+condition: service_healthy").ShouldBeFalse();
    }

    [Theory]
    [InlineData("webapi", "WEBAPI_SECRETS_GID")]
    [InlineData("consumer", "CONSUMER_SECRETS_GID")]
    [InlineData("jobs", "JOBS_SECRETS_GID")]
    [InlineData("database-migrator", "DATABASE_MIGRATOR_SECRETS_GID")]
    public void GiveEachServiceOnlyItsOwnSecretGenerationAndGroup(string service, string groupVariable)
    {
        var block = ReadServiceBlock(service);

        block.ShouldContain(service == "database-migrator"
            ? "DOTNET_ENVIRONMENT: Production"
            : "ASPNETCORE_ENVIRONMENT: Production");
        block.ShouldContain($"/{service}/current");
        block.ShouldContain(groupVariable);
        Regex.Matches(block, @"/(webapi|consumer|jobs|database-migrator)/current").Count.ShouldBe(1);
        Regex.Matches(block, "target: /run/secrets/app").Count.ShouldBe(1);
        block.ShouldContain("read_only: true");
        block.ShouldContain("create_host_path: false");
    }

    [Fact]
    public void KeepRuntimeCredentialsOutOfProductionEnvironment()
    {
        var compose = ReadProductionCompose();

        compose.ShouldNotContain("ConnectionStrings__");
        compose.ShouldNotContain("Jwt__SigningKey");
        compose.ShouldNotContain("Messaging__RabbitMq__UserName");
        compose.ShouldNotContain("Messaging__RabbitMq__Password");
        compose.ShouldNotContain("ObjectStorage__CloudflareR2__AccessKeyId");
        compose.ShouldNotContain("ObjectStorage__CloudflareR2__SecretAccessKey");
        compose.ShouldNotContain("OTEL_EXPORTER_OTLP_HEADERS");
        compose.ShouldNotContain("PYROSCOPE_BASIC_AUTH");
        Regex.IsMatch(compose, "^ {6}GITHUB_(USERNAME|PAT):", RegexOptions.Multiline).ShouldBeFalse();
    }

    private static string ReadServiceBlock(string service)
    {
        var compose = ReadProductionCompose().ReplaceLineEndings("\n");
        var match = Regex.Match(compose, $@"^  {Regex.Escape(service)}:\n(?<body>(?:(?:    .*)?\n)*)", RegexOptions.Multiline);
        match.Success.ShouldBeTrue($"Production Compose must define the '{service}' service.");
        return match.Groups["body"].Value;
    }

    [Fact]
    public void KeepLocalComposeOnTheNonProductionContainerEnvironment()
    {
        // Production would demand /run/secrets/app locally; Development enables DI ValidateOnBuild, which the
        // Consumer, Jobs and DatabaseMigrator registrations do not currently pass.
        var compose = ReadComposeFile("docker-compose.yml");

        Regex.Matches(compose, "ASPNETCORE_ENVIRONMENT: Container").Count.ShouldBe(3);
        Regex.Matches(compose, "DOTNET_ENVIRONMENT: Container").Count.ShouldBe(1);
        compose.ShouldNotContain("_ENVIRONMENT: Production");
        compose.ShouldNotContain("_ENVIRONMENT: Development");
        compose.ShouldNotContain("/run/secrets/app");
    }

    private static string ReadProductionCompose() => ReadComposeFile("docker-compose.production.yml");

    private static string ReadComposeFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, fileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {fileName} from the test output directory.");
    }
}
