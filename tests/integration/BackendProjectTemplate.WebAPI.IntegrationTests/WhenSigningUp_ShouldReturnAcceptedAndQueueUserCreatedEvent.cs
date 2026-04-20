using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI;
using BackendProjectTemplate.WebAPI.Features.Authentication.Registrations;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenSigningUp_ShouldReturnAcceptedAndQueueUserCreatedEvent(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private bool _createdCountryForTest;
    private SignUpRequest _request = default!;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
        await EnsureDefaultStakeholderTypeExistsAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteAuthenticationRecordsAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task Verify()
    {
        GivenANewEmailAddress();
        await WhenSigningUp();
        await ThenTheRequestIsAcceptedAndUserCreatedEventIsQueued();

        void GivenANewEmailAddress()
        {
            _email = WebApiIntegrationTestData.Email();
            _request = new SignUpRequest(
                _email,
                Password,
                Password,
                _countryId,
                WebApiIntegrationTestData.FirstName(),
                WebApiIntegrationTestData.LastName());
        }

        async Task WhenSigningUp()
        {
            _response = await Client.PostAsJsonAsync(EndpointUrl.Registrations.V1, _request);
        }

        async Task ThenTheRequestIsAcceptedAndUserCreatedEventIsQueued()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
            var outboxMessages = await repository.ListAsync(new UserCreatedOutboxMessagesSpecification());

            outboxMessages.Count.ShouldBe(1);
            outboxMessages[0].SentAtUtc.ShouldBeNull();
        }
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            return;
        }

        using var scope = CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await userRepository.GetByEmailAsync(_email);

        if (user is not null)
        {
            var stakeholders = await stakeholderRepository.ListAsync(new StakeholderByAppUserIdCleanupSpecification(user.Id));
            foreach (var stakeholder in stakeholders)
            {
                stakeholderRepository.Remove(stakeholder);
            }
        }

        if (user is not null)
        {
            userRepository.Remove(user);
        }

        var outboxMessages = await outboxRepository.ListAsync(new UserCreatedOutboxMessagesSpecification());
        foreach (var outboxMessage in outboxMessages)
        {
            outboxRepository.Remove(outboxMessage);
        }

        var stakeholderTypes = await stakeholderTypeRepository.ListAsync(new DefaultCustomerStakeholderTypeCleanupSpecification(_tenantId));
        foreach (var stakeholderType in stakeholderTypes)
        {
            stakeholderTypeRepository.Remove(stakeholderType);
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(_countryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
            }
        }

        if (user is not null || outboxMessages.Count > 0 || stakeholderTypes.Count > 0 || _createdCountryForTest)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }

    private async Task<Guid> ResolveCountryIdAsync()
    {
        using var scope = CreateScope();
        var countryReadRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var countryWriteRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var countries = await countryReadRepository.ListAsync(new FirstCountrySpecification());
        if (countries.Count > 0)
        {
            return countries[0].Id;
        }

        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg", now);
        await countryWriteRepository.AddAsync(country);
        await unitOfWork.SaveChangesAsync();
        _createdCountryForTest = true;

        return country.Id;
    }

    private async Task EnsureDefaultStakeholderTypeExistsAsync()
    {
        using var scope = CreateScope();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer", now);

        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await unitOfWork.SaveChangesAsync();
    }

    private sealed class UserCreatedOutboxMessagesSpecification : Specification<OutboxMessage>
    {
        public UserCreatedOutboxMessagesSpecification()
        {
            Where(message =>
                message.Kind == OutboxMessageKind.Event &&
                message.Type == typeof(UserCreated).FullName!);
        }
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification()
        {
            ApplyPaging(0, 1);
        }
    }

    private sealed class DefaultCustomerStakeholderTypeCleanupSpecification : Specification<StakeholderType>
    {
        public DefaultCustomerStakeholderTypeCleanupSpecification(Guid tenantId)
        {
            Where(stakeholderType => stakeholderType.TenantId == tenantId && stakeholderType.Key == "customer");
        }
    }

    private sealed class StakeholderByAppUserIdCleanupSpecification : Specification<Stakeholder>
    {
        public StakeholderByAppUserIdCleanupSpecification(Guid appUserId)
        {
            Where(stakeholder => stakeholder.AppUserId == appUserId);
        }
    }
}
