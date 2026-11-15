using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class RedisStepUpMfaProofStore(
    IConnectionMultiplexer connectionMultiplexer,
    TimeProvider timeProvider) : IStepUpMfaProofStore
{
    private static readonly TimeSpan MarkerRetention = TimeSpan.FromMinutes(5);
    private const int TokenGenerationAttempts = 3;
    private const string ConsumedMarker = "consumed";
    private const string ExpiredMarker = "expired";
    private const string IssuedMarkerPrefix = "issued:";

    public async Task<StepUpMfaProofIssueResult> IssueAsync(
        StepUpMfaProof proof,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lifetime = proof.ExpiresAtUtc - timeProvider.GetUtcNow();
        if (lifetime <= TimeSpan.Zero)
            throw new InvalidOperationException("Cannot issue an already-expired step-up MFA proof.");

        var database = connectionMultiplexer.GetDatabase();
        var serializedProof = JsonSerializer.Serialize(proof);

        for (var attempt = 0; attempt < TokenGenerationAttempts; attempt++)
        {
            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var tokenHash = Hash(token);
            var created = await database.StringSetAsync(
                ProofKey(tokenHash),
                serializedProof,
                lifetime,
                When.NotExists);

            if (!created)
                continue;

            await database.StringSetAsync(
                MarkerKey(tokenHash),
                IssuedMarkerPrefix + proof.ExpiresAtUtc.ToUnixTimeMilliseconds(),
                lifetime + MarkerRetention);

            return new StepUpMfaProofIssueResult(token, proof.ExpiresAtUtc);
        }

        throw new InvalidOperationException("Could not generate a unique step-up MFA proof token.");
    }

    public async Task<StepUpMfaProofConsumptionStatus> ConsumeAsync(
        string authorizationToken,
        StepUpMfaProofConsumption expected,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(authorizationToken))
            return StepUpMfaProofConsumptionStatus.Invalid;

        var tokenHash = Hash(authorizationToken);
        var database = connectionMultiplexer.GetDatabase();
        var serializedProof = await database.StringGetDeleteAsync(ProofKey(tokenHash));
        if (serializedProof.IsNull)
            return await ResolveMissingProofAsync(database, tokenHash);

        StepUpMfaProof? proof;
        try
        {
            proof = JsonSerializer.Deserialize<StepUpMfaProof>(serializedProof.ToString());
        }
        catch (JsonException)
        {
            await SetTerminalMarkerAsync(database, tokenHash, ConsumedMarker);
            return StepUpMfaProofConsumptionStatus.Invalid;
        }

        if (proof is null)
        {
            await SetTerminalMarkerAsync(database, tokenHash, ConsumedMarker);
            return StepUpMfaProofConsumptionStatus.Invalid;
        }

        if (proof.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            await SetTerminalMarkerAsync(database, tokenHash, ExpiredMarker);
            return StepUpMfaProofConsumptionStatus.Expired;
        }

        await SetTerminalMarkerAsync(database, tokenHash, ConsumedMarker);

        if (proof.AppUserId != expected.AppUserId || proof.StakeholderId != expected.StakeholderId)
            return StepUpMfaProofConsumptionStatus.ActorMismatch;
        if (proof.SessionId != expected.SessionId)
            return StepUpMfaProofConsumptionStatus.SessionMismatch;
        if (!string.Equals(proof.Operation, expected.Operation, StringComparison.Ordinal))
            return StepUpMfaProofConsumptionStatus.OperationMismatch;
        if (!string.Equals(proof.RequestFingerprint, expected.RequestFingerprint, StringComparison.Ordinal))
            return StepUpMfaProofConsumptionStatus.RequestMismatch;
        if (!string.Equals(proof.SecurityStamp, expected.SecurityStamp, StringComparison.Ordinal))
            return StepUpMfaProofConsumptionStatus.SecurityStampMismatch;

        return StepUpMfaProofConsumptionStatus.Consumed;
    }

    public async Task<bool> TryClaimAuthenticatorCodeAsync(
        Guid appUserId,
        string authenticatorCode,
        TimeSpan replayWindow,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var replayVerifier = Hash($"{appUserId:D}:{NormalizeAuthenticatorCode(authenticatorCode)}");
        return await connectionMultiplexer.GetDatabase().StringSetAsync(
            $"authentication:step-up-mfa:totp:{replayVerifier}",
            "used",
            replayWindow,
            When.NotExists);
    }

    private async Task<StepUpMfaProofConsumptionStatus> ResolveMissingProofAsync(
        IDatabase database,
        string tokenHash)
    {
        var marker = await database.StringGetAsync(MarkerKey(tokenHash));
        if (marker == ConsumedMarker)
            return StepUpMfaProofConsumptionStatus.AlreadyConsumed;
        if (marker == ExpiredMarker)
            return StepUpMfaProofConsumptionStatus.Expired;

        var markerText = marker.ToString();
        if (markerText.StartsWith(IssuedMarkerPrefix, StringComparison.Ordinal)
            && long.TryParse(markerText.AsSpan(IssuedMarkerPrefix.Length), out var expiresAtUnixMilliseconds))
        {
            return timeProvider.GetUtcNow().ToUnixTimeMilliseconds() >= expiresAtUnixMilliseconds
                ? StepUpMfaProofConsumptionStatus.Expired
                : StepUpMfaProofConsumptionStatus.AlreadyConsumed;
        }

        return StepUpMfaProofConsumptionStatus.Invalid;
    }

    private static Task<bool> SetTerminalMarkerAsync(
        IDatabase database,
        string tokenHash,
        string marker) =>
        database.StringSetAsync(MarkerKey(tokenHash), marker, MarkerRetention);

    private static string NormalizeAuthenticatorCode(string code) =>
        string.Concat(code.Where(char.IsDigit));

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static RedisKey ProofKey(string tokenHash) => $"authentication:step-up-mfa:proof:{tokenHash}";
    private static RedisKey MarkerKey(string tokenHash) => $"authentication:step-up-mfa:proof-marker:{tokenHash}";
}
