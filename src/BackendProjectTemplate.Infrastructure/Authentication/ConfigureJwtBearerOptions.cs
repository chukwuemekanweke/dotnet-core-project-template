using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class ConfigureJwtBearerOptions(
    IOptions<JwtOptions> jwtOptions,
    IAccessTokenRevocationService accessTokenRevocationService)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options) =>
        Configure(Options.DefaultName, options);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        var resolvedJwtOptions = jwtOptions.Value;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(resolvedJwtOptions.SigningKey));

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = resolvedJwtOptions.Issuer,
            ValidAudience = resolvedJwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var tokenId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(tokenId))
                {
                    context.Fail("Access token identifier is missing.");
                    return;
                }

                if (await accessTokenRevocationService.IsRevokedAsync(tokenId, context.HttpContext.RequestAborted))
                {
                    context.Fail("Access token has been revoked.");
                }
            }
        };
    }
}
