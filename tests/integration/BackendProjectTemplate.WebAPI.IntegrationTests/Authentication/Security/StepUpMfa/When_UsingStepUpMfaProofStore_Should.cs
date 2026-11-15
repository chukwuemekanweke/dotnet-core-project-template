using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Infrastructure.Authentication;
using Shouldly;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Authentication.Security.StepUpMfa;

[Collection(nameof(ContainersCollection))]
public sealed class When_UsingStepUpMfaProofStore_Should(ContainersFixture fixture) : IAsyncLifetime
{
    private IConnectionMultiplexer _connection = default!;
    private MutableTimeProvider _clock = default!;
    private RedisStepUpMfaProofStore _sut = default!;

    public Task InitializeAsync()
    {
        _connection = ConnectionMultiplexer.Connect(fixture.RedisConnectionString);
        _clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero));
        _sut = new RedisStepUpMfaProofStore(_connection, _clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task ConsumeExactlyOnceAcrossConcurrentRequestsWithoutStoringRawToken()
    {
        var proof = CreateProof();
        var issued = await _sut.IssueAsync(proof, CancellationToken.None);
        var expected = Expected(proof);

        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => _sut.ConsumeAsync(issued.AuthorizationToken, expected, CancellationToken.None)));

        results.Count(result => result == StepUpMfaProofConsumptionStatus.Consumed).ShouldBe(1);
        results.Count(result => result == StepUpMfaProofConsumptionStatus.AlreadyConsumed).ShouldBe(19);
        var server = _connection.GetServers().Single();
        server.Keys(pattern: "authentication:step-up-mfa:*")
            .Any(key => key.ToString().Contains(issued.AuthorizationToken, StringComparison.Ordinal))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task RejectMismatchedBindingsAndConsumeEachProof()
    {
        var proof = CreateProof();
        var issued = await _sut.IssueAsync(proof, CancellationToken.None);
        var expected = Expected(proof);

        (await _sut.ConsumeAsync(
            issued.AuthorizationToken,
            expected with { SessionId = Guid.CreateVersion7() },
            CancellationToken.None)).ShouldBe(StepUpMfaProofConsumptionStatus.SessionMismatch);
        (await _sut.ConsumeAsync(
            issued.AuthorizationToken,
            expected,
            CancellationToken.None)).ShouldBe(StepUpMfaProofConsumptionStatus.AlreadyConsumed);

        issued = await _sut.IssueAsync(proof, CancellationToken.None);
        (await _sut.ConsumeAsync(
            issued.AuthorizationToken,
            expected with { Operation = StepUpMfaOperations.ActivateProvider },
            CancellationToken.None)).ShouldBe(StepUpMfaProofConsumptionStatus.OperationMismatch);

        issued = await _sut.IssueAsync(proof, CancellationToken.None);
        (await _sut.ConsumeAsync(
            issued.AuthorizationToken,
            expected with { RequestFingerprint = new string('b', 64) },
            CancellationToken.None)).ShouldBe(StepUpMfaProofConsumptionStatus.RequestMismatch);
    }

    [Fact]
    public async Task ExpireAfterTwoMinutesAndRejectAuthenticatorCodeReplay()
    {
        var proof = CreateProof();
        var issued = await _sut.IssueAsync(proof, CancellationToken.None);

        (await _sut.TryClaimAuthenticatorCodeAsync(
            proof.AppUserId, "123 456", TimeSpan.FromMinutes(2), CancellationToken.None)).ShouldBeTrue();
        (await _sut.TryClaimAuthenticatorCodeAsync(
            proof.AppUserId, "123456", TimeSpan.FromMinutes(2), CancellationToken.None)).ShouldBeFalse();

        _clock.Advance(TimeSpan.FromMinutes(2));
        (await _sut.ConsumeAsync(
            issued.AuthorizationToken,
            Expected(proof),
            CancellationToken.None)).ShouldBe(StepUpMfaProofConsumptionStatus.Expired);
    }

    private StepUpMfaProof CreateProof()
    {
        var now = _clock.GetUtcNow();
        return new StepUpMfaProof(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7().ToString("N"),
            StepUpMfaOperations.AllocateSubscription,
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Guid.CreateVersion7().ToString()))),
            now,
            now.AddMinutes(2));
    }

    private static StepUpMfaProofConsumption Expected(StepUpMfaProof proof) => new(
        proof.AppUserId,
        proof.StakeholderId,
        proof.SessionId,
        proof.SecurityStamp,
        proof.Operation,
        proof.RequestFingerprint);

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
