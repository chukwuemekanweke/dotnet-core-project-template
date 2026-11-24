using BackendProjectTemplate.WebAPI.Infrastructure.ForwardedHeaders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.ForwardedHeaders;

public sealed class When_AddingTrustedForwardedHeaders_WithInvalidForwardLimit_Should
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Throw(string forwardLimit)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ForwardedHeaders:ForwardLimit"] = forwardLimit })
            .Build();

        var exception = Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddTrustedForwardedHeaders(configuration));

        exception.Message.ShouldContain("ForwardLimit");
    }
}
