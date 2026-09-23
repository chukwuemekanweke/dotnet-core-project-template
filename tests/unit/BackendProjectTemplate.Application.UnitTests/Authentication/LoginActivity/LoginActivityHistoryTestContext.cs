using BackendProjectTemplate.Application.Authentication.Features.GetLoginActivityHistory;
using BackendProjectTemplate.Domain.Authentication.ReadModels;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.LoginActivity;

internal sealed class LoginActivityHistoryTestContext
{
    public ILoginActivityReadModelRepository Repository { get; } = Substitute.For<ILoginActivityReadModelRepository>();

    public GetLoginActivityHistoryHandler CreateHandler() => new(Repository);
}
