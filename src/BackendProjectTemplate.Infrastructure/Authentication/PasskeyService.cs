using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class PasskeyService(
    IPasskeyHandler<AppUser> passkeyHandler,
    UserManager<AppUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IJsonCache cache,
    IOptions<PasskeyAuthenticationOptions> options,
    TimeProvider timeProvider) : IPasskeyService
{
    private static readonly TimeSpan MarkerRetention = TimeSpan.FromMinutes(5);
    private readonly PasskeyAuthenticationOptions _options = options.Value;

    public async Task<PasskeyOptions> CreateRegistrationOptionsAsync(
        AppUser user,
        Guid stakeholderId,
        Guid tenantId,
        string passkeyName,
        CancellationToken cancellationToken)
    {
        var result = await passkeyHandler.MakeCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = user.Id.ToString("D"),
            Name = user.Email ?? user.UserName ?? user.Id.ToString("D"),
            DisplayName = user.Email ?? user.UserName ?? _options.RelyingPartyName
        }, GetHttpContext());
        var flow = RegistrationFlow.Create(
            result.AttestationState ?? throw new InvalidOperationException("Identity did not return passkey attestation state."),
            user.Id,
            stakeholderId,
            tenantId,
            passkeyName.Trim(),
            timeProvider.GetUtcNow().Add(_options.ChallengeLifetime));
        await cache.SetAsync(RegistrationKey(flow.Token), flow, _options.ChallengeLifetime, cancellationToken);
        await cache.SetAsync(
            RegistrationMarkerKey(flow.Token),
            new FlowMarker(PasskeyOperationStatus.ExpiredFlow),
            _options.ChallengeLifetime + MarkerRetention,
            cancellationToken);
        return new PasskeyOptions(flow.Token, SetRelyingPartyName(result.CreationOptionsJson), flow.ExpiresAtUtc);
    }

    public async Task<PasskeyRegistrationResult> CompleteRegistrationAsync(
        AppUser user,
        Guid stakeholderId,
        Guid tenantId,
        string flowToken,
        string credentialJson,
        CancellationToken cancellationToken)
    {
        var taken = await TakeRegistrationAsync(flowToken, cancellationToken);
        if (taken.Status != PasskeyOperationStatus.Success)
            return new PasskeyRegistrationResult(taken.Status);

        var flow = taken.Flow!;
        if (flow.AppUserId != user.Id || flow.StakeholderId != stakeholderId || flow.TenantId != tenantId)
            return new PasskeyRegistrationResult(PasskeyOperationStatus.InvalidFlow);

        var result = await passkeyHandler.PerformAttestationAsync(new PasskeyAttestationContext
        {
            HttpContext = GetHttpContext(),
            CredentialJson = credentialJson,
            AttestationState = flow.AttestationState
        });
        if (!result.Succeeded || result.Passkey is null || result.UserEntity?.Id != user.Id.ToString("D"))
            return new PasskeyRegistrationResult(PasskeyOperationStatus.InvalidCredential);
        if (await userManager.FindByPasskeyIdAsync(result.Passkey.CredentialId) is not null)
            return new PasskeyRegistrationResult(PasskeyOperationStatus.DuplicateCredential);

        result.Passkey.Name = flow.PasskeyName;
        var persisted = await userManager.AddOrUpdatePasskeyAsync(user, result.Passkey);
        return persisted.Succeeded
            ? new PasskeyRegistrationResult(PasskeyOperationStatus.Success, Map(result.Passkey))
            : new PasskeyRegistrationResult(PasskeyOperationStatus.Failed);
    }

    public async Task<PasskeyOptions> CreateAuthenticationOptionsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await passkeyHandler.MakeRequestOptionsAsync(null!, GetHttpContext());
        var flow = AuthenticationFlow.Create(
            result.AssertionState ?? throw new InvalidOperationException("Identity did not return passkey assertion state."),
            tenantId,
            timeProvider.GetUtcNow().Add(_options.ChallengeLifetime));
        await cache.SetAsync(AuthenticationKey(flow.Token), flow, _options.ChallengeLifetime, cancellationToken);
        await cache.SetAsync(
            AuthenticationMarkerKey(flow.Token),
            new FlowMarker(PasskeyOperationStatus.ExpiredFlow),
            _options.ChallengeLifetime + MarkerRetention,
            cancellationToken);
        return new PasskeyOptions(flow.Token, result.RequestOptionsJson, flow.ExpiresAtUtc);
    }

    public async Task<PasskeyAuthenticationResult> VerifyAuthenticationAsync(
        Guid tenantId,
        string flowToken,
        string credentialJson,
        CancellationToken cancellationToken)
    {
        var taken = await TakeAuthenticationAsync(flowToken, cancellationToken);
        if (taken.Status != PasskeyOperationStatus.Success)
            return new PasskeyAuthenticationResult(taken.Status);
        if (taken.Flow!.TenantId != tenantId)
            return new PasskeyAuthenticationResult(PasskeyOperationStatus.InvalidFlow);

        var result = await passkeyHandler.PerformAssertionAsync(new PasskeyAssertionContext
        {
            HttpContext = GetHttpContext(),
            CredentialJson = credentialJson,
            AssertionState = taken.Flow.AssertionState
        });
        if (!result.Succeeded || result.User is null || result.Passkey is null)
            return new PasskeyAuthenticationResult(PasskeyOperationStatus.InvalidCredential);

        return new PasskeyAuthenticationResult(PasskeyOperationStatus.Success, result.User, result.Passkey);
    }

    public async Task<PasskeyOperationStatus> UpdateAuthenticationStateAsync(AppUser user, UserPasskeyInfo passkey) =>
        (await userManager.AddOrUpdatePasskeyAsync(user, passkey)).Succeeded
            ? PasskeyOperationStatus.Success
            : PasskeyOperationStatus.Failed;

    public async Task<IReadOnlyList<PasskeyCredential>> ListAsync(AppUser user) =>
        (await userManager.GetPasskeysAsync(user)).Select(Map).OrderByDescending(passkey => passkey.CreatedAtUtc).ToArray();

    public async Task<PasskeyOperationStatus> RenameAsync(AppUser user, string credentialId, string name)
    {
        if (!TryDecodeCredentialId(credentialId, out var id))
            return PasskeyOperationStatus.NotFound;
        var passkey = await userManager.GetPasskeyAsync(user, id);
        if (passkey is null)
            return PasskeyOperationStatus.NotFound;
        passkey.Name = name.Trim();
        return (await userManager.AddOrUpdatePasskeyAsync(user, passkey)).Succeeded
            ? PasskeyOperationStatus.Success
            : PasskeyOperationStatus.Failed;
    }

    public async Task<PasskeyOperationStatus> RemoveAsync(AppUser user, string credentialId)
    {
        if (!TryDecodeCredentialId(credentialId, out var id) || await userManager.GetPasskeyAsync(user, id) is null)
            return PasskeyOperationStatus.NotFound;
        return (await userManager.RemovePasskeyAsync(user, id)).Succeeded
            ? PasskeyOperationStatus.Success
            : PasskeyOperationStatus.Failed;
    }

    private HttpContext GetHttpContext() =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("Passkey operations require an active HTTP request.");

    private async Task<RegistrationTakeResult> TakeRegistrationAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new RegistrationTakeResult(PasskeyOperationStatus.InvalidFlow);
        var flow = await cache.GetAndRemoveAsync<RegistrationFlow>(RegistrationKey(token), cancellationToken);
        if (flow is not null)
        {
            var status = flow.ExpiresAtUtc <= timeProvider.GetUtcNow()
                ? PasskeyOperationStatus.ExpiredFlow
                : PasskeyOperationStatus.Success;
            await SetMarkerAsync(
                RegistrationMarkerKey(token),
                status == PasskeyOperationStatus.Success ? PasskeyOperationStatus.ConsumedFlow : status,
                cancellationToken);
            return new RegistrationTakeResult(status, status == PasskeyOperationStatus.Success ? flow : null);
        }
        return new RegistrationTakeResult(await GetMarkerStatusAsync(RegistrationMarkerKey(token), cancellationToken));
    }

    private async Task<AuthenticationTakeResult> TakeAuthenticationAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationTakeResult(PasskeyOperationStatus.InvalidFlow);
        var flow = await cache.GetAndRemoveAsync<AuthenticationFlow>(AuthenticationKey(token), cancellationToken);
        if (flow is not null)
        {
            var status = flow.ExpiresAtUtc <= timeProvider.GetUtcNow()
                ? PasskeyOperationStatus.ExpiredFlow
                : PasskeyOperationStatus.Success;
            await SetMarkerAsync(
                AuthenticationMarkerKey(token),
                status == PasskeyOperationStatus.Success ? PasskeyOperationStatus.ConsumedFlow : status,
                cancellationToken);
            return new AuthenticationTakeResult(status, status == PasskeyOperationStatus.Success ? flow : null);
        }
        return new AuthenticationTakeResult(await GetMarkerStatusAsync(AuthenticationMarkerKey(token), cancellationToken));
    }

    private async Task<PasskeyOperationStatus> GetMarkerStatusAsync(string key, CancellationToken cancellationToken)
    {
        var marker = await cache.GetAsync<FlowMarker>(key, cancellationToken);
        return marker?.Status ?? PasskeyOperationStatus.InvalidFlow;
    }

    private Task SetMarkerAsync(string key, PasskeyOperationStatus status, CancellationToken cancellationToken) =>
        cache.SetAsync(key, new FlowMarker(status), MarkerRetention, cancellationToken);

    private static PasskeyCredential Map(UserPasskeyInfo passkey) => new(
        WebEncoders.Base64UrlEncode(passkey.CredentialId),
        string.IsNullOrWhiteSpace(passkey.Name) ? "Passkey" : passkey.Name,
        passkey.CreatedAt,
        passkey.Transports ?? [],
        passkey.IsBackupEligible,
        passkey.IsBackedUp);

    private static bool TryDecodeCredentialId(string value, out byte[] id)
    {
        try
        {
            id = WebEncoders.Base64UrlDecode(value);
            return id.Length > 0;
        }
        catch (FormatException)
        {
            id = [];
            return false;
        }
    }

    private static string RegistrationKey(string token) => $"authentication:passkey-registration:{token}";
    private static string RegistrationMarkerKey(string token) => $"authentication:passkey-registration-marker:{token}";
    private static string AuthenticationKey(string token) => $"authentication:passkey-sign-in:{token}";
    private static string AuthenticationMarkerKey(string token) => $"authentication:passkey-sign-in-marker:{token}";

    private sealed record RegistrationFlow(
        string Token,
        string AttestationState,
        Guid AppUserId,
        Guid StakeholderId,
        Guid TenantId,
        string PasskeyName,
        DateTimeOffset ExpiresAtUtc)
    {
        public static RegistrationFlow Create(
            string state,
            Guid appUserId,
            Guid stakeholderId,
            Guid tenantId,
            string passkeyName,
            DateTimeOffset expiresAtUtc) =>
            new(CreateToken(), state, appUserId, stakeholderId, tenantId, passkeyName, expiresAtUtc);
    }

    private sealed record AuthenticationFlow(string Token, string AssertionState, Guid TenantId, DateTimeOffset ExpiresAtUtc)
    {
        public static AuthenticationFlow Create(string state, Guid tenantId, DateTimeOffset expiresAtUtc) =>
            new(CreateToken(), state, tenantId, expiresAtUtc);
    }

    private static string CreateToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private string SetRelyingPartyName(string optionsJson)
    {
        var optionsNode = JsonNode.Parse(optionsJson)?.AsObject()
            ?? throw new InvalidOperationException("Identity returned invalid passkey creation options.");
        var relyingParty = optionsNode["rp"]?.AsObject()
            ?? throw new InvalidOperationException("Identity returned passkey creation options without a relying party.");
        relyingParty["name"] = _options.RelyingPartyName;
        return optionsNode.ToJsonString();
    }

    private sealed record FlowMarker(PasskeyOperationStatus Status);
    private sealed record RegistrationTakeResult(PasskeyOperationStatus Status, RegistrationFlow? Flow = null);
    private sealed record AuthenticationTakeResult(PasskeyOperationStatus Status, AuthenticationFlow? Flow = null);
}
