using BackendProjectTemplate.Infrastructure.Authentication;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Authentication;

public sealed class When_ValidatingEnabledGoogleAuthentication_WithoutClientIds_Should
{
    [Fact]
    public void FailClearly()
    {
        var options = new GoogleAuthenticationOptions { Enabled = true };

        var exception = Should.Throw<InvalidOperationException>(options.Validate);

        exception.Message.ShouldContain("client id");
    }
}
