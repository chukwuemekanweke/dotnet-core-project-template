using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenValidatingPasskeyOptions_Should
{
    [Fact]
    public void AcceptExactLoopbackAndHttpsOrigins()
    {
        var options = new PasskeyAuthenticationOptions
        {
            RelyingPartyId = "example.com",
            AllowedOrigins = ["http://localhost:3000", "https://app.example.com"]
        };

        Should.NotThrow(options.Validate);
    }

    [Fact]
    public void RejectWildcardOrigin()
    {
        var options = new PasskeyAuthenticationOptions
        {
            RelyingPartyId = "example.com",
            AllowedOrigins = ["https://*.example.com"]
        };

        Should.Throw<InvalidOperationException>(options.Validate);
    }
}
