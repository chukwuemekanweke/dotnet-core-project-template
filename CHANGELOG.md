# Changelog

## Unreleased

### Added

- Added isolated KeyPerFile production configuration and fail-closed secret validation for all four .NET hosts.
- Added DEVOPS-007 key manifests and a standalone production Compose contract.
- Added a startup guard that fails Production hosts when a configuration provider is registered after KeyPerFile.

### Changed

- Restricted the template JWT signing key to Development and removed runtime GitHub credentials from local Compose services.
- Ran every local Compose host in the non-Production `Container` environment, with an overridable dev-only JWT key for WebAPI.
- Validated the JWT signing key length on WebAPI startup in every environment and rejected the Development key in Production.
- Made integration tests fail instead of skipping when Docker is unavailable.
- Required production secrets to be resolved from the KeyPerFile provider itself, so trailing newlines and DEVOPS-007 metadata files are handled consistently.
