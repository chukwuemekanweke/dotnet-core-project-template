using BackendProjectTemplate.WebAPI.Infrastructure.CrossOrigin;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.CrossOrigin;

public sealed class When_ConfiguringApiCors_Should
{
    [Fact]
    public void AllowOnlyConfiguredOriginsInProduction()
    {
        var policy = BuildDefaultPolicy(Environments.Production, new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://template-user.chidelu.com/",
            ["Cors:AllowedOrigins:1"] = " https://Template-Admin.chidelu.com ",
            ["Cors:AllowedOrigins:2"] = "https://template-user.chidelu.com"
        });

        policy.AllowAnyOrigin.ShouldBeFalse();
        policy.Origins.ShouldBe(["https://template-user.chidelu.com", "https://template-admin.chidelu.com"], ignoreOrder: true);
    }

    [Fact]
    public void KeepLocalDefaultInProductionWhenNothingIsConfigured()
    {
        var policy = BuildDefaultPolicy(Environments.Production, new Dictionary<string, string?>());

        policy.Origins.ShouldBe(["http://localhost:9000", "https://localhost:9000"], ignoreOrder: true);
    }

    [Fact]
    public void AllowAnyOriginOutsideProduction()
    {
        var policy = BuildDefaultPolicy(Environments.Development, new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://template-user.chidelu.com"
        });

        policy.AllowAnyOrigin.ShouldBeTrue();
    }

    [Theory]
    [InlineData("template-user.chidelu.com")]
    [InlineData("https://template-user.chidelu.com/app")]
    [InlineData("ftp://template-user.chidelu.com")]
    [InlineData("https://template-user.chidelu.com?x=1")]
    public void RejectInvalidOrigin(string origin)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin })
            .Build();

        var exception = Should.Throw<InvalidOperationException>(
            () => new ServiceCollection().AddApiCors(configuration, Environment(Environments.Production)));

        exception.Message.ShouldContain(origin);
    }

    private static CorsPolicy BuildDefaultPolicy(string environmentName, Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        using var provider = new ServiceCollection()
            .AddApiCors(configuration, Environment(environmentName))
            .BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CorsOptions>>().Value;

        return options.GetPolicy(options.DefaultPolicyName).ShouldNotBeNull();
    }

    private static IHostEnvironment Environment(string name)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(name);
        return environment;
    }
}
