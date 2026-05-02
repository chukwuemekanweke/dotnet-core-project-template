using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Payments.WalletTransactions.TopUpDetails;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingWalletTopUpTransactionDetail_WithExistingTransaction_Should(ContainersFixture fixture) : IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private readonly CustomWebApplicationFactory _factory = new(
        fixture.PostgresConnectionString,
        fixture.RedisConnectionString);

    private HttpClient _client = default!;
    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _currencyId;
    private Guid _walletId;
    private Guid _paymentProviderId;
    private Guid _paymentTransactionId;
    private Guid _walletTransactionId;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _tenantId = Guid.CreateVersion7();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        await CreateVerifiedUserAsync();
        await SeedWalletTopUpTransactionAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ReturnWalletTopUpTransactionDetail()
    {
        await WhenGettingWalletTopUpTransactionDetail();

        _response.ShouldNotBeNull();
        _response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await _response.Content.ReadFromJsonAsync<GetStakeholderWalletTopUpTransactionDetailResponse>();
        payload.ShouldNotBeNull();
        payload.WalletTransactionId.ShouldBe(_walletTransactionId);
        payload.TransactionTitle.ShouldBe(WalletTransactionTitles.WalletFunding);
        payload.Description.ShouldBe(WalletTransactionNarratives.WalletFunding.CreateDescription());
        payload.Amount.ShouldBe(2500m);
        payload.CurrencyCode.ShouldBe("NGN");
        payload.MerchantReference.ShouldBe("merchant-ref-1");
        payload.PaymentMethodType.ShouldBe(nameof(PaymentMethodType.BankTransfer));
        payload.PaymentProviderName.ShouldBe("SafeHaven");

        async Task WhenGettingWalletTopUpTransactionDetail()
        {
            _response = await _client.GetAsync(EndpointUrl.Payments.WalletTopUpTransactionDetailsV1(_walletTransactionId));
        }
    }

    private async Task AuthenticateAsync()
    {
        var signInResponse = await _client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password));
        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedUserAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Domain.ReferenceData.Entities.Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        _email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(_email, "Ada", "Lovelace", now);
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified(now);
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var country = Domain.ReferenceData.Entities.Country.Create("Nigeria", "NG", "+234", "https://example.com/ng.svg", now);
        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer", now);
        var stakeholder = Stakeholder.Create(user.Id, _tenantId, country.Id, stakeholderType.Id, "Ada", "Lovelace", now);

        await countryRepository.AddAsync(country);
        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        _countryId = country.Id;
        _stakeholderTypeId = stakeholderType.Id;
        _stakeholderId = stakeholder.Id;
    }

    private async Task SeedWalletTopUpTransactionAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackendProjectTemplate.Infrastructure.Persistence.AppDbContext>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();

        var currency = Currency.Create("NGN", "Naira", true, now);
        var wallet = Wallet.Create(_stakeholderId, _tenantId, currency.Id, now);
        wallet.Credit(2500m);
        var paymentProvider = PaymentProvider.Create("SafeHaven", PaymentProviderKeys.SafeHaven, true, now);
        var paymentTransaction = PaymentTransaction.Create(
            "merchant-ref-1",
            PaymentIntent.WalletTopUp,
            paymentProvider.Id,
            2500m,
            currency.Id,
            _countryId,
            Guid.CreateVersion7(),
            _stakeholderId,
            _tenantId,
            now);
        paymentTransaction.SetPaymentMethodType(PaymentMethodType.BankTransfer);
        paymentTransaction.MarkInitiated("provider-ref-1", null, now.AddMinutes(15), "Payment initiated.");

        var walletTransaction = WalletTransaction.CreateCredit(
            wallet.Id,
            paymentTransaction.Id,
            "merchant-ref-1",
            2500m,
            currency.Id,
            now,
            WalletTransactionCategory.WalletFunding,
            WalletTransactionNarratives.WalletFunding.Title,
            WalletTransactionNarratives.WalletFunding.CreateDescription());

        await dbContext.Currencies.AddAsync(currency);
        await dbContext.Wallets.AddAsync(wallet);
        await dbContext.PaymentProviders.AddAsync(paymentProvider);
        await dbContext.PaymentTransactions.AddAsync(paymentTransaction);
        await dbContext.WalletTransactions.AddAsync(walletTransaction);
        await dbContext.SaveChangesAsync();

        _currencyId = currency.Id;
        _walletId = wallet.Id;
        _paymentProviderId = paymentProvider.Id;
        _paymentTransactionId = paymentTransaction.Id;
        _walletTransactionId = walletTransaction.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackendProjectTemplate.Infrastructure.Persistence.AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();

        var walletTransaction = await dbContext.WalletTransactions.FirstOrDefaultAsync(item => item.Id == _walletTransactionId);
        if (walletTransaction is not null)
        {
            dbContext.WalletTransactions.Remove(walletTransaction);
        }

        var paymentTransaction = await dbContext.PaymentTransactions.FirstOrDefaultAsync(item => item.Id == _paymentTransactionId);
        if (paymentTransaction is not null)
        {
            dbContext.PaymentTransactions.Remove(paymentTransaction);
        }

        var paymentProvider = await dbContext.PaymentProviders.FirstOrDefaultAsync(item => item.Id == _paymentProviderId);
        if (paymentProvider is not null)
        {
            dbContext.PaymentProviders.Remove(paymentProvider);
        }

        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(item => item.Id == _walletId);
        if (wallet is not null)
        {
            dbContext.Wallets.Remove(wallet);
        }

        var currency = await dbContext.Currencies.FirstOrDefaultAsync(item => item.Id == _currencyId);
        if (currency is not null)
        {
            dbContext.Currencies.Remove(currency);
        }

        var stakeholder = await dbContext.Stakeholders.FirstOrDefaultAsync(item => item.Id == _stakeholderId);
        if (stakeholder is not null)
        {
            dbContext.Stakeholders.Remove(stakeholder);
        }

        var stakeholderType = await dbContext.StakeholderTypes.FirstOrDefaultAsync(item => item.Id == _stakeholderTypeId);
        if (stakeholderType is not null)
        {
            dbContext.StakeholderTypes.Remove(stakeholderType);
        }

        var country = await dbContext.Countries.FirstOrDefaultAsync(item => item.Id == _countryId);
        if (country is not null)
        {
            dbContext.Countries.Remove(country);
        }

        var user = await userRepository.GetByEmailAsync(_email);
        if (user is not null)
        {
            userRepository.Remove(user);
        }

        await dbContext.SaveChangesAsync();
    }

    private IServiceScope CreateScope() => _factory.Services.CreateScope();
}
