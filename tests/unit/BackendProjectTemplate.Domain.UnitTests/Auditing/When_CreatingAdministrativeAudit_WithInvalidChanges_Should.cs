namespace BackendProjectTemplate.Domain.UnitTests.Auditing;

public sealed class When_CreatingAdministrativeAudit_WithInvalidChanges_Should
{
    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"isActive\":true}")]
    [InlineData("{\"isActive\":{\"current\":true}}")]
    [InlineData("{\"isActive\":{\"previous\":false,\"current\":true,\"extra\":1}}")]
    [InlineData("{\"IsActive\":{\"previous\":false,\"current\":true}}")]
    public void RejectPayloadsOutsideTheChangeStructure(string changes) =>
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(changes: changes));
}
