namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
