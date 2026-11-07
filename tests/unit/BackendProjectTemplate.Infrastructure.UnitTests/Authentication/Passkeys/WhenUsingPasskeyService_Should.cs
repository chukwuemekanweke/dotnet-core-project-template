using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Caching;
using BackendProjectTemplate.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using System.Collections.Concurrent;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Authentication.Passkeys;

public sealed class WhenUsingPasskeyService_Should
{
    [Fact]
    public async Task PersistValidatedRegistration()
    {
        var context = new PasskeyServiceTestContext();
        var options = await context.Service.CreateRegistrationOptionsAsync(
            context.User,
            context.StakeholderId,
            context.TenantId,
            "Laptop",
            CancellationToken.None);

        var result = await context.Service.CompleteRegistrationAsync(
            context.User,
            context.StakeholderId,
            context.TenantId,
            options.FlowToken,
            "{}",
            CancellationToken.None);

        result.Status.ShouldBe(PasskeyOperationStatus.Success);
        result.Passkey.ShouldNotBeNull();
        result.Passkey.Name.ShouldBe("Laptop");
        await context.UserManager.Received(1).AddOrUpdatePasskeyAsync(context.User, context.Passkey);
    }

    [Fact]
    public async Task DeferCredentialStatePersistenceUntilExplicitlyRequested()
    {
        var context = new PasskeyServiceTestContext();
        var options = await context.Service.CreateAuthenticationOptionsAsync(context.TenantId, CancellationToken.None);

        var result = await context.Service.VerifyAuthenticationAsync(
            context.TenantId,
            options.FlowToken,
            "{}",
            CancellationToken.None);

        result.Status.ShouldBe(PasskeyOperationStatus.Success);
        result.User.ShouldBe(context.User);
        result.Passkey.ShouldBe(context.Passkey);
        await context.UserManager.DidNotReceiveWithAnyArgs()
            .AddOrUpdatePasskeyAsync(default!, default!);

        var update = await context.Service.UpdateAuthenticationStateAsync(result.User!, result.Passkey!);

        update.ShouldBe(PasskeyOperationStatus.Success);
        await context.UserManager.Received(1).AddOrUpdatePasskeyAsync(context.User, context.Passkey);
    }

    [Fact]
    public async Task RejectFailedAssertionValidation()
    {
        var context = new PasskeyServiceTestContext();
        var options = await context.Service.CreateAuthenticationOptionsAsync(context.TenantId, CancellationToken.None);
        context.PasskeyHandler.PerformAssertionAsync(Arg.Any<PasskeyAssertionContext>()).Returns(
            PasskeyAssertionResult.Fail<AppUser>(new PasskeyException("invalid assertion")));

        var result = await context.Service.VerifyAuthenticationAsync(
            context.TenantId,
            options.FlowToken,
            "{}",
            CancellationToken.None);

        result.Status.ShouldBe(PasskeyOperationStatus.InvalidCredential);
        await context.UserManager.DidNotReceiveWithAnyArgs()
            .AddOrUpdatePasskeyAsync(default!, default!);
    }

    [Fact]
    public async Task AllowOnlyOneConcurrentCompletion()
    {
        var context = new PasskeyServiceTestContext();
        var options = await context.Service.CreateAuthenticationOptionsAsync(context.TenantId, CancellationToken.None);

        var results = await Task.WhenAll(
            context.Service.VerifyAuthenticationAsync(
                context.TenantId, options.FlowToken, "{}", CancellationToken.None),
            context.Service.VerifyAuthenticationAsync(
                context.TenantId, options.FlowToken, "{}", CancellationToken.None));

        results.Count(result => result.Status == PasskeyOperationStatus.Success).ShouldBe(1);
        results.Count(result => result.Status == PasskeyOperationStatus.ConsumedFlow).ShouldBe(1);
        await context.PasskeyHandler.Received(1).PerformAssertionAsync(Arg.Any<PasskeyAssertionContext>());
    }

    [Fact]
    public async Task RemoveOwnedCredential()
    {
        var context = new PasskeyServiceTestContext();
        var credentialId = WebEncoders.Base64UrlEncode(context.Passkey.CredentialId);

        var result = await context.Service.RemoveAsync(context.User, credentialId);

        result.ShouldBe(PasskeyOperationStatus.Success);
        await context.UserManager.Received(1).RemovePasskeyAsync(
            context.User,
            Arg.Is<byte[]>(value => value.SequenceEqual(context.Passkey.CredentialId)));
    }

    private sealed class PasskeyServiceTestContext
    {
        private readonly TimeProvider _clock = new FixedTimeProvider(
            new DateTimeOffset(2026, 11, 7, 0, 0, 0, TimeSpan.Zero));

        public Guid TenantId { get; } = Guid.CreateVersion7();
        public Guid StakeholderId { get; } = Guid.CreateVersion7();
        public AppUser User { get; } = AppUser.Create("passkey@example.com");
        public UserPasskeyInfo Passkey { get; }
        public IPasskeyHandler<AppUser> PasskeyHandler { get; } = Substitute.For<IPasskeyHandler<AppUser>>();
        public UserManager<AppUser> UserManager { get; }
        public PasskeyService Service { get; }

        public PasskeyServiceTestContext()
        {
            Passkey = new UserPasskeyInfo(
                [1], [2], _clock.GetUtcNow(), 2, ["internal"], true, false, false, [3], [4]);
            UserManager = CreateUserManager();
            UserManager.AddOrUpdatePasskeyAsync(User, Passkey).Returns(IdentityResult.Success);
            UserManager.FindByPasskeyIdAsync(Passkey.CredentialId).Returns((AppUser?)null);
            UserManager.GetPasskeyAsync(User, Arg.Is<byte[]>(value => value.SequenceEqual(Passkey.CredentialId)))
                .Returns(Passkey);
            UserManager.RemovePasskeyAsync(User, Arg.Is<byte[]>(value => value.SequenceEqual(Passkey.CredentialId)))
                .Returns(IdentityResult.Success);
            PasskeyHandler.MakeCreationOptionsAsync(Arg.Any<PasskeyUserEntity>(), Arg.Any<HttpContext>()).Returns(
                new PasskeyCreationOptionsResult
                {
                    CreationOptionsJson = "{\"challenge\":\"challenge\",\"rp\":{\"id\":\"localhost\",\"name\":\"localhost\"}}",
                    AttestationState = "attestation-state"
                });
            PasskeyHandler.PerformAttestationAsync(Arg.Any<PasskeyAttestationContext>()).Returns(
                PasskeyAttestationResult.Success(Passkey, new PasskeyUserEntity
                {
                    Id = User.Id.ToString("D"),
                    Name = User.Email!,
                    DisplayName = User.Email!
                }));
            PasskeyHandler.MakeRequestOptionsAsync(null!, Arg.Any<HttpContext>()).Returns(
                new PasskeyRequestOptionsResult
                {
                    RequestOptionsJson = "{\"challenge\":\"challenge\",\"rpId\":\"localhost\"}",
                    AssertionState = "assertion-state"
                });
            PasskeyHandler.PerformAssertionAsync(Arg.Any<PasskeyAssertionContext>()).Returns(
                PasskeyAssertionResult.Success(Passkey, User));
            Service = new PasskeyService(
                PasskeyHandler,
                UserManager,
                new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
                new InMemoryJsonCache(_clock),
                Options.Create(new PasskeyAuthenticationOptions()),
                _clock);
        }

        private static UserManager<AppUser> CreateUserManager()
        {
            var store = Substitute.For<IUserStore<AppUser>>();
            return Substitute.For<UserManager<AppUser>>(
                store,
                Options.Create(new IdentityOptions()),
                Substitute.For<IPasswordHasher<AppUser>>(),
                Array.Empty<IUserValidator<AppUser>>(),
                Array.Empty<IPasswordValidator<AppUser>>(),
                Substitute.For<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Substitute.For<IServiceProvider>(),
                Substitute.For<ILogger<UserManager<AppUser>>>());
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemoryJsonCache(TimeProvider timeProvider) : IJsonCache
    {
        private readonly ConcurrentDictionary<string, CacheItem> _items = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) =>
            Task.FromResult(_items.TryGetValue(key, out var item) && item.ExpiresAtUtc > timeProvider.GetUtcNow()
                ? (T?)item.Value
                : default);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _items[key] = new CacheItem(value!, timeProvider.GetUtcNow().Add(ttl));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            _items.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<T?> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken) =>
            Task.FromResult(_items.TryRemove(key, out var item) && item.ExpiresAtUtc > timeProvider.GetUtcNow()
                ? (T?)item.Value
                : default);

        private sealed record CacheItem(object Value, DateTimeOffset ExpiresAtUtc);
    }
}
