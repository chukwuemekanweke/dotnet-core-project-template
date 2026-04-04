using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

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
            ConfirmPassword = "P@ssw0rd123!",
            FirstName = "Ada",
            LastName = "Lovelace"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        factory.OtpDeliveryService.GetCode(email).ShouldNotBeNull();
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
            ConfirmPassword = "P@ssw0rd123!",
            FirstName = "Grace",
            LastName = "Hopper"
        });

        var verifyResponse = await client.PostAsJsonAsync("/api/authentication/sign-up/otp", new SignUpOtpRequest
        {
            Email = email,
            Otp = factory.OtpDeliveryService.GetCode(email)!
        });

        verifyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
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
            ConfirmPassword = password,
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

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        string.IsNullOrWhiteSpace(payload?.AccessToken).ShouldBeFalse();
    }
}
