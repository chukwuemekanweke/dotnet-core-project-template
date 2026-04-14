# AGENTS

This repository is a `.NET 10` backend template. Any agent working here should follow the conventions below.

## Verification

- Always run `build` and `test` sequentially.
- Never run `dotnet build` and `dotnet test` in parallel in this repository.
- Reason: parallel execution can leave `testhost` processes holding DLL locks and cause transient copy failures during build.
- Preferred verification flow:
  1. `dotnet build BackendProjectTemplate.slnx --no-restore`
  2. `dotnet test BackendProjectTemplate.slnx --no-build`

## Architecture

The solution uses DDD project boundaries with vertical slices inside the application and WebAPI layers.

### Project boundaries

- `src/BackendProjectTemplate.Domain`
  Contains domain entities, value objects, domain interfaces, and domain-level persistence contracts.
- `src/BackendProjectTemplate.Application`
  Contains application use cases organized by feature.
- `src/BackendProjectTemplate.Infrastructure`
  Contains implementations for persistence, caching, identity integrations, API clients, and other external dependencies.
- `src/BackendProjectTemplate.WebAPI`
  Contains the HTTP host and controller layer only.
- `src/BackendProjectTemplate.Consumer`
  Contains the async subscriber host.
- `src/BackendProjectTemplate.Jobs`
  Contains the scheduled job host.
- `src/BackendProjectTemplate.DatabaseMigrator`
  Contains database migration, pre-deploy, and post-deploy execution logic.

### DDD rules

- Infrastructure concerns live in `Infrastructure`.
- Interfaces for infrastructure-backed behavior live in `Domain`.
- Do not place caching, API client implementations, EF Core implementations, Redis code, or other external dependency code in `Application` or `WebAPI`.
- Domain abstractions should stay narrow and reflect only what the application currently needs.

### Vertical slice rules

- Organize application code by feature, not by technical type.
- Each feature gets its own folder.
- Keep command, response, handler, and result in separate files within the same application feature folder.
- Keep WebAPI request DTOs and request validators in the matching controller feature folder at the HTTP edge.
- Keep feature-specific helper classes in the same feature folder when they are only used by that feature.
- WebAPI controllers should also be organized by feature folder.
- Prefer controller-based endpoints, not minimal APIs.
- Routes and controllers should use resource-oriented naming.
- Prefer `record` over `class` for data transfer objects.
- Treat commands, requests, responses, results, and similar transport/application DTOs as records by default.
- Prefer positional record syntax for DTOs, for example `public sealed record SignInCommand(string Email, string Password);`

### Current feature layout expectation

Example:

`src/BackendProjectTemplate.Application/Authentication/Features/SignUp`

Expected files in a feature folder:

- `SignUpCommand.cs`
- `SignUpResponse.cs`
- `SignUpResult.cs`
- `SignUpValidator.cs`
- `SignUpHandler.cs`

Matching WebAPI controller location:

`src/BackendProjectTemplate.WebAPI/Features/Authentication/Registrations/RegistrationsController.cs`

Matching WebAPI request DTO location:

`src/BackendProjectTemplate.WebAPI/Features/Authentication/Registrations/SignUpRequest.cs`

## Testing

### Test project layout

- Use separate unit test projects for:
  - `Application`
  - `Domain`
  - `Infrastructure`
  - `WebAPI`
  - `Consumer`
  - `Jobs`
- Use separate integration test projects for:
  - `WebAPI`
  - `Consumer`
  - `Jobs`
- Within a test project, mirror the production project structure where practical.
- Example: `Jobs` tests should group scenarios under folders like `HealthChecks`, `OutboxProcessing`, and `Infrastructure` instead of keeping all files flat.

### Unit test rules

- Use `NSubstitute` for mocks, substitutes, and fakes.
- Use `Shouldly` for assertions.
- Keep one test case per file.
- Name unit test files and classes with `When_X_Should_Y`.
- Prefer method-local scenario variables over repeated string literals.
- Reuse helper factory methods or test context builders where available.
- Keep unit tests focused on the behavior of the unit under test, not infrastructure wiring.

### Integration test rules

- Integration tests should cover the happy path only.
- Focus on the path that gives the most meaningful end-to-end code-path coverage.
- Keep one endpoint scenario per file.
- Each integration test class must implement `IAsyncLifetime`.
- Use `InitializeAsync` for:
  - seeding data required by the scenario
  - creating prerequisite records
  - creating any SQL view, function, or stored procedure needed by the test
- Use `DisposeAsync` for:
  - deleting records created or touched by the test
  - clearing test doubles or in-memory stores used by the test
- Cleanup should be explicit and targeted.
- Do not use vague global wipes when only a small set of records were touched.
- Use `Given / When / Then` structure only inside the method under test, typically as local anonymous functions inside `Verify()`.
- Helper methods outside `Verify()` should use normal verb-based names.

### Current integration testing conventions

- Testcontainers are used for integration dependencies.
- Shared container setup stays in the project fixture.
- Per-test data setup and cleanup belongs in the test class via `IAsyncLifetime`.
- WebAPI auth integration tests should delete the authentication records for the email used by the scenario.
- Consumer and Jobs integration tests currently validate health endpoints and normally do not create application records, so their cleanup is usually limited to response disposal and host disposal.

## Additional repository conventions

- Use `TimeProvider` for time-related behavior instead of direct `DateTime.UtcNow` in application code.
- Use `Guid.CreateVersion7()` for generated GUID values across application and test code instead of `Guid.NewGuid()`.
- In method signatures, use `CancellationToken cancellationToken` and do not use a default value (do not write `= default`).
- ASP.NET Core Identity is the authentication base. Do not reintroduce custom authentication flows when built-in Identity behavior is sufficient.
- Keep application dependencies narrow. If wrapping `UserManager`, expose only the methods the application currently uses.
