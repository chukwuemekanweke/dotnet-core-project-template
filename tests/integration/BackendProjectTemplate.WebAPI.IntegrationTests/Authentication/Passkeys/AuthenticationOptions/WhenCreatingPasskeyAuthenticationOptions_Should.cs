using BackendProjectTemplate.WebAPI.Features.Authentication.Passkeys;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenCreatingPasskeyAuthenticationOptions_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.CreateVersion7().ToString());
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnBrowserCompatibleOptionsAndOpaqueFlow()
    {
        PasskeyOptionsResponse? payload = null;

        await WhenOptionsAreRequested();
        ThenOptionsAndFlowAreReturned();

        async Task WhenOptionsAreRequested()
        {
            _response = await Client.PostAsync(EndpointUrl.Passkeys.AuthenticationOptionsV1, null);
            payload = await _response.Content.ReadFromJsonAsync<PasskeyOptionsResponse>();
        }

        void ThenOptionsAndFlowAreReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
            string.IsNullOrWhiteSpace(payload?.FlowToken).ShouldBeFalse();
            payload!.ExpiresAtUtc.ShouldBeGreaterThan(TimeProvider.System.GetUtcNow());
            payload.PublicKey.TryGetProperty("challenge", out _).ShouldBeTrue();
            payload.PublicKey.TryGetProperty("rpId", out _).ShouldBeTrue();
        }
    }
}
