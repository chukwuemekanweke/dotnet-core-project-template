using BackendProjectTemplate.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Configuration;

public sealed class When_LoadingProductionSecrets_Should
{
    [Fact]
    public void NotRequireSecretDirectoryOutsideProduction()
    {
        var builder = CreateBuilder(Environments.Development);

        Should.NotThrow(() => builder.AddProductionSecrets(
            "Test.Application",
            [ProductionSecretRequirement.Required("Test:Secret")],
            Path.Combine(Path.GetTempPath(), Guid.CreateVersion7().ToString())));
    }

    [Fact]
    public void FailWhenProductionSecretDirectoryIsMissing()
    {
        var builder = CreateBuilder(Environments.Production);

        var exception = Should.Throw<InvalidOperationException>(() => builder.AddProductionSecrets(
            "Test.Application",
            [ProductionSecretRequirement.Required("Test:Secret")],
            Path.Combine(Path.GetTempPath(), Guid.CreateVersion7().ToString())));

        exception.Message.ShouldContain("Test.Application");
        exception.Message.ShouldContain("does not exist");
    }

    [Fact]
    public void FailWhenRequiredFileIsMissingEvenIfEnvironmentValueExists()
    {
        using var directory = new TemporarySecretDirectory();
        var builder = CreateBuilder(Environments.Production);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Test:Secret"] = "environment-only-value"
        });

        var exception = Should.Throw<InvalidOperationException>(() => builder.AddProductionSecrets(
            "Test.Application",
            [ProductionSecretRequirement.Required("Test:Secret")],
            directory.Path));

        exception.Message.ShouldContain("Test:Secret");
        exception.Message.ShouldContain("missing");
        exception.Message.ShouldNotContain("environment-only-value");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \t")]
    public void FailWhenRequiredFileIsBlank(string value)
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("TEST__SECRET", value);
        var builder = CreateBuilder(Environments.Production);

        var exception = Should.Throw<InvalidOperationException>(() => builder.AddProductionSecrets(
            "Test.Application",
            [ProductionSecretRequirement.Required("Test:Secret")],
            directory.Path));

        exception.Message.ShouldContain("Test:Secret");
        exception.Message.ShouldContain("blank");
        if (value.Length > 0)
        {
            exception.Message.ShouldNotContain(value);
        }
    }

    [Fact]
    public void MapDoubleUnderscoreToNestedConfigurationAndOverrideExistingProviders()
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("TEST__SECRET", "file-value");
        var builder = CreateBuilder(Environments.Production);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Test:Secret"] = "appsettings-value"
        });
        var environmentPrefix = $"KPF{Guid.CreateVersion7():N}_";
        Environment.SetEnvironmentVariable($"{environmentPrefix}Test__Secret", "environment-value");
        try
        {
            builder.Configuration.AddEnvironmentVariables(environmentPrefix);
            builder.Configuration["Test:Secret"].ShouldBe("environment-value");

            builder.AddProductionSecrets(
                "Test.Application",
                [ProductionSecretRequirement.Required("Test:Secret")],
                directory.Path);

            builder.Configuration["Test:Secret"].ShouldBe("file-value");
        }
        finally
        {
            Environment.SetEnvironmentVariable($"{environmentPrefix}Test__Secret", null);
        }
    }

    [Fact]
    public void KeepStartupSnapshotWhenSecretFileChanges()
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("TEST__SECRET", "initial-value");
        var builder = CreateBuilder(Environments.Production);
        builder.AddProductionSecrets(
            "Test.Application",
            [ProductionSecretRequirement.Required("Test:Secret")],
            directory.Path);

        directory.Write("TEST__SECRET", "rotated-value");

        builder.Configuration["Test:Secret"].ShouldBe("initial-value");
    }

    [Fact]
    public void RejectKnownJwtPlaceholder()
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("JWT__SIGNINGKEY", "super-secret-template-signing-key-change-me");
        var builder = CreateBuilder(Environments.Production);

        var exception = Should.Throw<InvalidOperationException>(() => builder.AddProductionSecrets(
            "Test.WebAPI",
            [ProductionSecretRequirement.JwtSigningKey()],
            directory.Path));

        exception.Message.ShouldContain("Jwt:SigningKey");
        exception.Message.ShouldContain("placeholder");
        exception.Message.ShouldNotContain("super-secret-template-signing-key-change-me");
    }

    [Fact]
    public void RejectJwtSigningKeyShorterThan256Bits()
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("JWT__SIGNINGKEY", "too-short");
        var builder = CreateBuilder(Environments.Production);

        var exception = Should.Throw<InvalidOperationException>(() => builder.AddProductionSecrets(
            "Test.WebAPI",
            [ProductionSecretRequirement.JwtSigningKey()],
            directory.Path));

        exception.Message.ShouldContain("at least 32 UTF-8 bytes");
        exception.Message.ShouldNotContain("too-short");
    }

    [Fact]
    public void AcceptStrongJwtSigningKey()
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("JWT__SIGNINGKEY", "a-production-key-with-at-least-32-bytes");
        var builder = CreateBuilder(Environments.Production);

        builder.AddProductionSecrets(
            "Test.WebAPI",
            [ProductionSecretRequirement.JwtSigningKey()],
            directory.Path);

        builder.Configuration["Jwt:SigningKey"].ShouldBe("a-production-key-with-at-least-32-bytes");
    }

    [Fact]
    public void RejectDevelopmentJwtSigningKeyInProduction()
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("JWT__SIGNINGKEY", "development-only-signing-key-not-for-production");
        var builder = CreateBuilder(Environments.Production);

        var exception = Should.Throw<InvalidOperationException>(() => builder.AddProductionSecrets(
            "Test.WebAPI",
            [ProductionSecretRequirement.JwtSigningKey()],
            directory.Path));

        exception.Message.ShouldContain("placeholder");
    }

    [Fact]
    public void TreatSecretFileWithTrailingNewlineAsTheExactValue()
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("TEST__SECRET", "file-value" + Environment.NewLine);
        var builder = CreateBuilder(Environments.Production);

        builder.AddProductionSecrets(
            "Test.Application",
            [ProductionSecretRequirement.Required("Test:Secret")],
            directory.Path);

        builder.Configuration["Test:Secret"].ShouldBe("file-value");
    }

    [Fact]
    public void IgnoreDevopsGenerationMetadataFile()
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("TEST__SECRET", "file-value");
        directory.Write(".metadata.json", "{\"keyCount\":1}");
        var builder = CreateBuilder(Environments.Production);

        builder.AddProductionSecrets(
            "Test.Application",
            [ProductionSecretRequirement.Required("Test:Secret")],
            directory.Path);

        builder.Configuration.AsEnumerable()
            .ShouldNotContain(entry => entry.Key.Contains("metadata", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FailHostStartupWhenProviderIsAddedAfterSecretProvider()
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("TEST__SECRET", "file-value");
        var builder = CreateBuilder(Environments.Production);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddProductionSecrets(
            "Test.Application",
            [ProductionSecretRequirement.Required("Test:Secret")],
            directory.Path);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Test:Secret"] = "late-override"
        });
        await using var app = builder.Build();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => app.StartAsync());

        exception.Message.ShouldContain("Test.Application");
        exception.Message.ShouldContain("highest precedence");
        exception.Message.ShouldNotContain("late-override");
        exception.Message.ShouldNotContain("file-value");
    }

    [Fact]
    public async Task StartHostWhenSecretProviderKeepsHighestPrecedence()
    {
        using var directory = new TemporarySecretDirectory();
        directory.Write("TEST__SECRET", "file-value");
        var builder = CreateBuilder(Environments.Production);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddProductionSecrets(
            "Test.Application",
            [ProductionSecretRequirement.Required("Test:Secret")],
            directory.Path);
        await using var app = builder.Build();

        await app.StartAsync();
        await app.StopAsync();

        app.Configuration["Test:Secret"].ShouldBe("file-value");
    }

    private static WebApplicationBuilder CreateBuilder(string environmentName) =>
        WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });

    private sealed class TemporarySecretDirectory : IDisposable
    {
        public TemporarySecretDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"production-secrets-{Guid.CreateVersion7()}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Write(string fileName, string value) =>
            File.WriteAllText(System.IO.Path.Combine(Path, fileName), value);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
