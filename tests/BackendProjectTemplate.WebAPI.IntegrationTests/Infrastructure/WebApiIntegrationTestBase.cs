using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

public abstract class WebApiIntegrationTestBase
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly string _sqlConnectionString;

    protected WebApiIntegrationTestBase(ContainersFixture fixture)
    {
        _factory = new CustomWebApplicationFactory(fixture.SqlConnectionString, fixture.RedisConnectionString);
        _sqlConnectionString = fixture.SqlConnectionString;
    }

    protected HttpClient Client { get; private set; } = default!;
    protected TestOtpDeliveryService OtpDeliveryService => _factory.OtpDeliveryService;

    protected Task InitializeClientAsync()
    {
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return Task.CompletedTask;
    }

    protected async Task DisposeClientAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    protected async Task DeleteAuthenticationUserByEmailAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        await using var connection = new SqlConnection(_sqlConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE userTokens
            FROM [authentication].[UserTokens] AS userTokens
            INNER JOIN [authentication].[Users] AS users ON users.[Id] = userTokens.[UserId]
            WHERE users.[Email] = @email;

            DELETE userLogins
            FROM [authentication].[UserLogins] AS userLogins
            INNER JOIN [authentication].[Users] AS users ON users.[Id] = userLogins.[UserId]
            WHERE users.[Email] = @email;

            DELETE userClaims
            FROM [authentication].[UserClaims] AS userClaims
            INNER JOIN [authentication].[Users] AS users ON users.[Id] = userClaims.[UserId]
            WHERE users.[Email] = @email;

            DELETE userRoles
            FROM [authentication].[UserRoles] AS userRoles
            INNER JOIN [authentication].[Users] AS users ON users.[Id] = userRoles.[UserId]
            WHERE users.[Email] = @email;

            DELETE FROM [authentication].[Users]
            WHERE [Email] = @email;
            """;

        command.Parameters.AddWithValue("@email", email);
        await command.ExecuteNonQueryAsync();
    }

    protected void ClearOtpDeliveries() => OtpDeliveryService.Clear();
}
