using BackendProjectTemplate.Domain.Kyc;
using Shouldly;
using System.Net;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Kyc;

public sealed class When_CreatingKycaidSession_WithDisabledIntegration_Should
{
    [Fact]
    public async Task ReportDefinitiveRejection()
    {
        var client = KycaidClientTestFactory.Create(HttpStatusCode.OK, enabled: false);

        await Should.ThrowAsync<KycProviderRequestRejectedException>(() =>
            client.CreateSessionAsync(new("external-1", null), CancellationToken.None));
    }
}
