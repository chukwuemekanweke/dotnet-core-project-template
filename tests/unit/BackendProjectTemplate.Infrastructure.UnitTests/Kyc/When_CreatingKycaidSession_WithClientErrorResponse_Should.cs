using BackendProjectTemplate.Domain.Kyc;
using Shouldly;
using System.Net;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Kyc;

public sealed class When_CreatingKycaidSession_WithClientErrorResponse_Should
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ReportDefinitiveRejection(HttpStatusCode statusCode)
    {
        var client = KycaidClientTestFactory.Create(statusCode);

        await Should.ThrowAsync<KycProviderRequestRejectedException>(() =>
            client.CreateSessionAsync(new("external-1", null), CancellationToken.None));
    }
}
