using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.IntegrationTests.Infrastructure;

namespace BackendProjectTemplate.IntegrationTests;

public sealed class AuthenticationEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task SignUp_ReturnsAccepted_AndStoresOtp()
    {
        var client = factory.CreateClient();
        var email = $"signup-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/authentication/sign-up", new SignUpRequest
        {
            Email = email,
            Password = "P@ssw0rd123!",
            FirstName = "Ada",
            LastName = "Lovelace"
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(factory.OtpDeliveryService.GetCode(email));
    }

    [Fact]
    public async Task VerifyOtp_ActivatesTheAccount()
    {
        var client = factory.CreateClient();
        var email = $"verify-{Guid.NewGuid():N}@example.com";

        await client.PostAsJsonAsync("/api/authentication/sign-up", new SignUpRequest
        {
            Email = email,
            Password = "P@ssw0rd123!",
            FirstName = "Grace",
            LastName = "Hopper"
        });

        var verifyResponse = await client.PostAsJsonAsync("/api/authentication/sign-up/otp", new SignUpOtpRequest
        {
            Email = email,
            Otp = factory.OtpDeliveryService.GetCode(email)!
        });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    [Fact]
    public async Task SignIn_ReturnsToken_AfterVerification()
    {
        var client = factory.CreateClient();
        var email = $"signin-{Guid.NewGuid():N}@example.com";
        const string password = "P@ssw0rd123!";

        await client.PostAsJsonAsync("/api/authentication/sign-up", new SignUpRequest
        {
            Email = email,
            Password = password,
            FirstName = "Linus",
            LastName = "Torvalds"
        });

        await client.PostAsJsonAsync("/api/authentication/sign-up/otp", new SignUpOtpRequest
        {
            Email = email,
            Otp = factory.OtpDeliveryService.GetCode(email)!
        });

        var signInResponse = await client.PostAsJsonAsync("/api/authentication/sign-in", new SignInRequest
        {
            Email = email,
            Password = password
        });

        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(payload?.AccessToken));
    }
}
