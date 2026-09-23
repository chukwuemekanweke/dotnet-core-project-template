using BackendProjectTemplate.Application.Authentication.Features.GetLoginActivityHistory;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.LoginActivity;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.LoginActivity;

internal sealed class LoginActivityControllerTestContext
{
    public ILoginActivityReadModelRepository Repository { get; } = Substitute.For<ILoginActivityReadModelRepository>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();

    public LoginActivityController CreateController() =>
        new(new GetLoginActivityHistoryHandler(Repository), new GetLoginActivityValidator(), CurrentActor);
}
