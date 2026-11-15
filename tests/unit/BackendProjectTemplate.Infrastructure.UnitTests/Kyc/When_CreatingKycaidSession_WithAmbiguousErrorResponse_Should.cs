using Shouldly;
using System.Net;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Kyc;

public sealed class When_CreatingKycaidSession_WithAmbiguousErrorResponse_Should
{
    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task ReportUnknownOutcome(HttpStatusCode statusCode)
    {
        var client = KycaidClientTestFactory.Create(statusCode);

        var exception = await Should.ThrowAsync<HttpRequestException>(() =>
            client.CreateSessionAsync(new("external-1", null), CancellationToken.None));

        exception.StatusCode.ShouldBe(statusCode);
    }
}
