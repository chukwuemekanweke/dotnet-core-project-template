using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

internal sealed class StepUpMfaTestAuthorizationHandler(IServiceProvider services) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<StepUpMfaOperationRegistry>();
        if (request.RequestUri is not null &&
            registry.TryResolveOperation(request.Method.Method, request.RequestUri.AbsolutePath, out var operation) &&
            TryResolveClaims(request, out var appUserId, out var stakeholderId, out var sessionId))
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await userManager.FindByIdAsync(appUserId.ToString());
            if (user is not null)
            {
                if (!await userManager.GetTwoFactorEnabledAsync(user))
                {
                    (await userManager.SetTwoFactorEnabledAsync(user, true)).Succeeded.ShouldBeTrue();
                }

                var canonicalizer = scope.ServiceProvider.GetRequiredService<StepUpMfaRequestCanonicalizer>();
                var body = await ReadBodyAsync(request, cancellationToken);
                var query = QueryHelpers.ParseQuery(request.RequestUri.Query).ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.Select(value => value ?? string.Empty).ToArray(),
                    StringComparer.Ordinal);
                canonicalizer.TryCreateFingerprint(
                    operation,
                    request.Method.Method,
                    request.RequestUri.AbsolutePath,
                    query,
                    body,
                    out var fingerprint).ShouldBeTrue();

                var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
                var now = clock.GetUtcNow();
                var proofStore = scope.ServiceProvider.GetRequiredService<IStepUpMfaProofStore>();
                var issued = await proofStore.IssueAsync(
                    new StepUpMfaProof(
                        appUserId,
                        stakeholderId,
                        sessionId,
                        await userManager.GetSecurityStampAsync(user),
                        operation,
                        fingerprint,
                        now,
                        now.AddMinutes(2)),
                    cancellationToken);
                request.Headers.Add(RequireStepUpMfaAttribute.HeaderName, issued.AuthorizationToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static bool TryResolveClaims(
        HttpRequestMessage request,
        out Guid appUserId,
        out Guid stakeholderId,
        out Guid sessionId)
    {
        appUserId = default;
        stakeholderId = default;
        sessionId = default;
        var token = request.Headers.Authorization?.Parameter;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var principal = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return Guid.TryParse(principal.Claims.FirstOrDefault(claim =>
                claim.Type is JwtRegisteredClaimNames.Sub or ClaimTypes.NameIdentifier)?.Value, out appUserId) &&
            Guid.TryParse(principal.Claims.FirstOrDefault(claim =>
                claim.Type == CustomClaimTypes.StakeholderId)?.Value, out stakeholderId) &&
            Guid.TryParse(principal.Claims.FirstOrDefault(claim =>
                claim.Type is JwtRegisteredClaimNames.Sid or ClaimTypes.Sid)?.Value, out sessionId);
    }

    private static async Task<JsonElement?> ReadBodyAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Content is null)
            return null;

        var json = await request.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
