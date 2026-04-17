using System.IdentityModel.Tokens.Jwt;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class WhenGeneratingAccessToken_ShouldIncludeStakeholderIdClaim
{
    [Fact]
    public void Verify()
    {
        var stakeholderId = Guid.CreateVersion7();
        var user = AppUser.Create(
            InfrastructureTestData.Email(),
            InfrastructureTestData.FirstName(),
            InfrastructureTestData.LastName(),
            new DateTimeOffset(2026, 4, 16, 0, 0, 0, TimeSpan.Zero));
        var sut = new JwtTokenGenerator(
            Options.Create(new JwtOptions
            {
                Issuer = "tests",
                Audience = "tests",
                SigningKey = "super-secret-template-signing-key-change-me"
            }),
            new FakeTimeProvider());

        var token = sut.Generate(user, stakeholderId);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        jwt.Claims.Single(claim => claim.Type == CustomClaimTypes.StakeholderId).Value.ShouldBe(stakeholderId.ToString());
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 4, 16, 0, 0, 0, TimeSpan.Zero);
    }
}
