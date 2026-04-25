using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using BackendProjectTemplate.WebAPI.Features.Payments.InitiatePayment;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Payments.InitiatePayment;

[Collection(nameof(ContainersCollection))]
public sealed class When_InitiatingPayment_WithSupportedProviderAndCurrency_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _currencyId;
    private Guid _countryCurrencyId;
    private Guid _paymentProviderId;
    private Guid _paymentProviderConfigurationId;
    private Guid _paymentTransactionId;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        await CreateVerifiedUserAsync();
        await SeedPaymentSetupAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnPaymentInstructions()
    {
        await WhenInitiatingPayment();
        await ThenThePaymentTransactionIsCreated();

        async Task WhenInitiatingPayment()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Payments.InitiateV1,
                new InitiatePaymentRequest(2500m, _currencyId, "WalletTopUp", _paymentProviderId));
        }

        async Task ThenThePaymentTransactionIsCreated()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var payload = await _response.Content.ReadFromJsonAsync<InitiatePaymentResponse>();
            payload.ShouldNotBeNull();
            payload.PaymentProviderId.ShouldBe(_paymentProviderId);
            payload.PaymentInstruction.Count.ShouldBeGreaterThan(0);

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BackendProjectTemplate.Infrastructure.Persistence.AppDbContext>();
            var transaction = await dbContext.PaymentTransactions
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstAsync(item => item.PaymentProviderId == _paymentProviderId);

            _paymentTransactionId = transaction.Id;
            transaction.PaymentStatus.ShouldBe(Contracts.Payments.PaymentStatus.Initiated);
            transaction.Amount.ShouldBe(2500m);
            transaction.StakeholderId.ShouldBe(_stakeholderId);
        }
    }

    private async Task AuthenticateAsync()
    {
        var signInResponse = await Client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password));
        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedUserAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        _email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(_email, "Ada", "Lovelace", now);
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified(now);
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var country = Country.Create("Nigeria", "NG", "+234", "https://example.com/ng.svg", now);
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

    private async Task SeedPaymentSetupAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackendProjectTemplate.Infrastructure.Persistence.AppDbContext>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var currency = Currency.Create("NGN", "Naira", true, now);
        var countryCurrency = CountryCurrency.Create(_countryId, currency.Id, true, true, now);
        var provider = PaymentProvider.Create("Credo", "credo", true, now);
        provider.SetConfiguration(currency.Id, Contracts.Payments.PaymentIntent.WalletTopUp, Contracts.Payments.PaymentMethodType.PaymentLink, true);

        await dbContext.Currencies.AddAsync(currency);
        await dbContext.CountryCurrencies.AddAsync(countryCurrency);
        await dbContext.PaymentProviders.AddAsync(provider);
        await dbContext.SaveChangesAsync();

        _currencyId = currency.Id;
        _countryCurrencyId = countryCurrency.Id;
        _paymentProviderId = provider.Id;
        _paymentProviderConfigurationId = provider.Configurations.Single().Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackendProjectTemplate.Infrastructure.Persistence.AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();

        if (_paymentTransactionId != Guid.Empty)
        {
            var transaction = await dbContext.PaymentTransactions.FirstOrDefaultAsync(item => item.Id == _paymentTransactionId);
            if (transaction is not null)
            {
                dbContext.PaymentTransactions.Remove(transaction);
            }
        }

        var providerConfiguration = await dbContext.PaymentProviderConfigurations.FirstOrDefaultAsync(item => item.Id == _paymentProviderConfigurationId);
        if (providerConfiguration is not null)
        {
            dbContext.PaymentProviderConfigurations.Remove(providerConfiguration);
        }

        var provider = await dbContext.PaymentProviders.FirstOrDefaultAsync(item => item.Id == _paymentProviderId);
        if (provider is not null)
        {
            dbContext.PaymentProviders.Remove(provider);
        }

        var countryCurrency = await dbContext.CountryCurrencies.FirstOrDefaultAsync(item => item.Id == _countryCurrencyId);
        if (countryCurrency is not null)
        {
            dbContext.CountryCurrencies.Remove(countryCurrency);
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
}
