using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class When_UpdatingStakeholderPreferences_WithUnsupportedLanguage_Should
{
    [Fact]
    public void RejectPreference()
    {
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Ada",
            "Lovelace");

        Should.Throw<ArgumentException>(() => stakeholder.UpdatePreferences("system", "de"));
    }
}
