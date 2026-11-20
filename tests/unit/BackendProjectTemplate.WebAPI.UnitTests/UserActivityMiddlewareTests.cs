using BackendProjectTemplate.Application.Analytics;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Analytics;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BackendProjectTemplate.WebAPI.UnitTests;

public sealed class UserActivityMiddlewareTests
{
    [Fact]
    public async Task When_AuthenticatedSuccessfulBusinessRequest_ShouldPublishObservation()
    {
        var context = new TestContext(StatusCodes.Status200OK, authenticated: true);

        await context.InvokeAsync();

        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Any<UserActivityObserved>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(StatusCodes.Status401Unauthorized, false)]
    [InlineData(StatusCodes.Status500InternalServerError, true)]
    public async Task When_RequestIsUnauthorizedOrFailed_ShouldNotPublish(
        int statusCode,
        bool authenticated)
    {
        var context = new TestContext(statusCode, authenticated);

        await context.InvokeAsync();

        await context.EventPublisher.DidNotReceiveWithAnyArgs().PublishAsync(default(UserActivityObserved)!, default);
    }

    [Fact]
    public async Task When_EndpointIsExcluded_ShouldNotPublish()
    {
        var context = new TestContext(StatusCodes.Status200OK, authenticated: true, excluded: true);

        await context.InvokeAsync();

        await context.EventPublisher.DidNotReceiveWithAnyArgs().PublishAsync(default(UserActivityObserved)!, default);
    }

    [Fact]
    public async Task When_HeaderTenantDoesNotOwnStakeholder_ShouldNotPublish()
    {
        var context = new TestContext(StatusCodes.Status200OK, authenticated: true, tenantMatches: false);

        await context.InvokeAsync();

        await context.EventPublisher.DidNotReceiveWithAnyArgs().PublishAsync(default(UserActivityObserved)!, default);
    }

    private sealed class TestContext
    {
        private readonly HttpContext _httpContext;
        private readonly ICurrentActor _currentActor = Substitute.For<ICurrentActor>();
        private readonly IStakeholderReadModelRepository _stakeholderRepository =
            Substitute.For<IStakeholderReadModelRepository>();
        private readonly UserActivityObservationService _observationService;
        private readonly UserActivityMiddleware _middleware;

        public TestContext(
            int statusCode,
            bool authenticated,
            bool excluded = false,
            bool tenantMatches = true)
        {
            var tenantId = Guid.CreateVersion7();
            var stakeholderId = Guid.CreateVersion7();
            var claim = Substitute.For<IUserActivityThrottle>();
            claim.TryClaimAsync(
                    Arg.Any<string>(),
                    Arg.Any<Guid>(),
                    Arg.Any<TimeSpan>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);
            var unitOfWork = Substitute.For<IUnitOfWork>();
            _observationService = new UserActivityObservationService(
                claim,
                EventPublisher,
                unitOfWork,
                Substitute.For<ILogger<UserActivityObservationService>>());

            _currentActor.ActorId.Returns(stakeholderId.ToString());
            _currentActor.TenantId.Returns(tenantId);
            _stakeholderRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
                .Returns(new StakeholderReadModel(
                    stakeholderId,
                    Guid.CreateVersion7(),
                    "test@example.com",
                    tenantMatches ? tenantId : Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    "Test",
                    "User",
                    null,
                    false));

            _httpContext = new DefaultHttpContext();
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                authenticated ? [new Claim(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString())] : [],
                authenticated ? "Bearer" : null));
            _httpContext.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                excluded ? new EndpointMetadataCollection(new ExcludeFromUserActivityAttribute()) : new EndpointMetadataCollection(),
                "test"));

            _middleware = new UserActivityMiddleware(
                context =>
                {
                    context.Response.StatusCode = statusCode;
                    return Task.CompletedTask;
                },
                Substitute.For<ILogger<UserActivityMiddleware>>());
        }

        public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();

        public Task InvokeAsync() => _middleware.InvokeAsync(
            _httpContext,
            _currentActor,
            _stakeholderRepository,
            _observationService,
            TimeProvider.System);
    }
}
