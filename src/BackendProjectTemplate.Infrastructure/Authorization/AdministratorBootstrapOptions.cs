namespace BackendProjectTemplate.Infrastructure.Authorization;

public sealed class AdministratorBootstrapOptions
{
    public const string SectionName = "Authorization:AdministratorBootstrap";

    public bool Enabled { get; init; }
    public Guid? UserId { get; init; }
}
