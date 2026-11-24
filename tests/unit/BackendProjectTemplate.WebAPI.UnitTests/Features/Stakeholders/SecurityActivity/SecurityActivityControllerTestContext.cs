using BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.SecurityActivity;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.SecurityActivity;

internal sealed class SecurityActivityControllerTestContext
{
    public ISecurityActivityReadModelRepository Repository { get; } = Substitute.For<ISecurityActivityReadModelRepository>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();

    public SecurityActivityController CreateController() =>
        new(new GetSecurityActivityHistoryHandler(Repository), new GetSecurityActivityValidator(), CurrentActor);
}
