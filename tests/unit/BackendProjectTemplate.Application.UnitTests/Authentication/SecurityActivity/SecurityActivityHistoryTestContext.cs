using BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;
using BackendProjectTemplate.Domain.Authentication.ReadModels;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.SecurityActivity;

internal sealed class SecurityActivityHistoryTestContext
{
    public ISecurityActivityReadModelRepository Repository { get; } = Substitute.For<ISecurityActivityReadModelRepository>();

    public GetSecurityActivityHistoryHandler CreateHandler() => new(Repository);
}
