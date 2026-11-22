using BackendProjectTemplate.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class When_ConfiguringJwtAuthentication_Should
{
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("shorter-than-32-bytes")]
    public void RejectMissingOrWeakSigningKey(string? signingKey)
    {
        using var provider = CreateServiceProvider(signingKey);

        var exception = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<JwtOptions>>().Value);

        exception.Message.ShouldContain("Jwt:SigningKey");
        if (!string.IsNullOrWhiteSpace(signingKey))
        {
            exception.Message.ShouldNotContain(signingKey);
        }
    }

    [Fact]
    public void AcceptSigningKeyOfAtLeast256Bits()
    {
        const string signingKey = "a-signing-key-with-at-least-32-utf8-bytes";
        using var provider = CreateServiceProvider(signingKey);

        provider.GetRequiredService<IOptions<JwtOptions>>().Value.SigningKey.ShouldBe(signingKey);
    }

    private static ServiceProvider CreateServiceProvider(string? signingKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = signingKey
            })
            .Build();

        return new ServiceCollection()
            .AddJwtAuthentication(configuration)
            .BuildServiceProvider();
    }
}
