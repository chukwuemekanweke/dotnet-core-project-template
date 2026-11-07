using BackendProjectTemplate.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Authentication.Passkeys;

public sealed class WhenValidatingPasskeyOrigins_Should
{
    [Theory]
    [InlineData("http://localhost:9000", false, true)]
    [InlineData("http://localhost:3000", false, false)]
    [InlineData("http://localhost:9000", true, false)]
    public async Task RequireAnExactAllowedTopLevelOrigin(
        string origin,
        bool crossOrigin,
        bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Passkeys:RelyingPartyId"] = "localhost",
                ["Authentication:Passkeys:RelyingPartyName"] = "Integration Tests",
                ["Authentication:Passkeys:AllowedOrigins:0"] = "http://localhost:9000"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityUserManagement(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<IdentityPasskeyOptions>>().Value;
        var validateOrigin = options.ValidateOrigin;
        validateOrigin.ShouldNotBeNull();

        var result = await validateOrigin(new PasskeyOriginValidationContext
        {
            HttpContext = new DefaultHttpContext(),
            Origin = origin,
            CrossOrigin = crossOrigin
        });

        result.ShouldBe(expected);
    }
}
