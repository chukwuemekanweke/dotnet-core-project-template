using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI;
using BackendProjectTemplate.WebAPI.Features.Authentication.PasswordResets;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenRequestingPasswordReset_ShouldQueueResetPasswordCommand(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private bool _createdCountryForTest;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
        await CreateUserWithStakeholderAsync();
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
        await WhenRequestingPasswordReset();
        await ThenTheRequestIsAcceptedAndResetPasswordCommandIsQueued();

        async Task WhenRequestingPasswordReset()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.PasswordResets.V1,
                new PasswordResetRequest(_email));
        }

        async Task ThenTheRequestIsAcceptedAndResetPasswordCommandIsQueued()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
            var outboxMessages = await repository.ListAsync(new ResetPasswordOutboxMessagesSpecification());

            outboxMessages.Count.ShouldBe(1);
            outboxMessages[0].SentAtUtc.ShouldBeNull();
        }
    }

    private async Task CreateUserWithStakeholderAsync()
    {
        _email = WebApiIntegrationTestData.Email();
        _firstName = WebApiIntegrationTestData.FirstName();
        _lastName = WebApiIntegrationTestData.LastName();
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var appUserStakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<AppUserStakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = timeProvider.GetUtcNow();
        var user = AppUser.Create(_email, _firstName, _lastName, now);
        var createResult = await identityService.CreateAsync(user, Password);
        createResult.Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer", now);
        var stakeholder = Stakeholder.Create(_tenantId, _countryId, stakeholderType.Id, _firstName, _lastName, now);
        var appUserStakeholder = AppUserStakeholder.Create(user.Id, stakeholder.Id, now);

        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await appUserStakeholderRepository.AddAsync(appUserStakeholder);
        await unitOfWork.SaveChangesAsync();

        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            return;
        }

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var appUserStakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<AppUserStakeholder>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await repository.GetByEmailAsync(_email);

        var appUserStakeholders = await appUserStakeholderRepository.ListAsync(new AppUserStakeholderByStakeholderIdCleanupSpecification(_stakeholderId));
        foreach (var link in appUserStakeholders)
        {
            appUserStakeholderRepository.Remove(link);
        }

        var stakeholders = await stakeholderRepository.ListAsync(new StakeholderByIdCleanupSpecification(_stakeholderId));
        foreach (var stakeholder in stakeholders)
        {
            stakeholderRepository.Remove(stakeholder);
        }

        var stakeholderTypes = await stakeholderTypeRepository.ListAsync(new StakeholderTypeByIdCleanupSpecification(_stakeholderTypeId));
        foreach (var stakeholderType in stakeholderTypes)
        {
            stakeholderTypeRepository.Remove(stakeholderType);
        }

        var outboxMessages = await outboxRepository.ListAsync(new ResetPasswordOutboxMessagesSpecification());
        foreach (var outboxMessage in outboxMessages)
        {
            outboxRepository.Remove(outboxMessage);
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(_countryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
            }
        }

        if (user is not null)
        {
            repository.Remove(user);
        }

        if (user is not null || appUserStakeholders.Count > 0 || stakeholders.Count > 0 || stakeholderTypes.Count > 0 || outboxMessages.Count > 0 || _createdCountryForTest)
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

    private sealed class ResetPasswordOutboxMessagesSpecification : Specification<OutboxMessage>
    {
        public ResetPasswordOutboxMessagesSpecification()
        {
            Where(message =>
                message.Kind == OutboxMessageKind.Command &&
                message.Type == typeof(ResetPasswordCommand).FullName!);
        }
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification()
        {
            ApplyPaging(0, 1);
        }
    }

    private sealed class AppUserStakeholderByStakeholderIdCleanupSpecification : Specification<AppUserStakeholder>
    {
        public AppUserStakeholderByStakeholderIdCleanupSpecification(Guid stakeholderId)
        {
            Where(link => link.StakeholderId == stakeholderId);
        }
    }

    private sealed class StakeholderByIdCleanupSpecification : Specification<Stakeholder>
    {
        public StakeholderByIdCleanupSpecification(Guid stakeholderId)
        {
            Where(stakeholder => stakeholder.Id == stakeholderId);
        }
    }

    private sealed class StakeholderTypeByIdCleanupSpecification : Specification<StakeholderType>
    {
        public StakeholderTypeByIdCleanupSpecification(Guid stakeholderTypeId)
        {
            Where(stakeholderType => stakeholderType.Id == stakeholderTypeId);
        }
    }
}
