# Repository Guidelines

## Project Structure & Module Organization
`Fantasy-server.sln` contains the API and test projects. Core application code lives in `Fantasy.Server/`, organized by domain under `Domain/Account`, `Domain/Auth`, and `Domain/Player`, with shared infrastructure in `Global/` and EF Core migrations in `Migrations/`. Local container setup lives in `Fantasy.Server/deploy/`. Tests live in `Fantasy.Test/`, mirroring the server by domain and layer, for example `Fantasy.Test/Auth/Service/`.

## Build, Test, and Development Commands
Use the .NET CLI from the repository root.

- `dotnet restore Fantasy-server.sln`: restore NuGet packages.
- `dotnet build Fantasy-server.sln`: compile the API and test project.
- `dotnet run --project Fantasy.Server`: start the API locally.
- `dotnet test Fantasy.Test`: run the xUnit suite.
- `dotnet test Fantasy.Test --collect:"XPlat Code Coverage"`: run tests with Coverlet coverage output.
- `docker compose -f Fantasy.Server/deploy/compose.dev.yaml up --build`: start PostgreSQL, Redis, and the API with the development settings.

## Coding Style & Naming Conventions
Use 4-space indentation and nullable-enabled C# conventions. Prefer `PascalCase` for types, methods, and properties, `camelCase` for locals and parameters, `_camelCase` for private fields, and `IPascalCase` for interfaces. Use `var` only when the type is obvious. DTOs should be positional `record` types in `Dto/Request` or `Dto/Response`; entities should expose `private set` and use static `Create(...)` factories. EF Core table names use `snake_case`, and configuration classes belong in `Domain/{Name}/Entity/Config/`.

## Testing Guidelines
The test stack is xUnit v3 with FluentAssertions, NSubstitute, and Coverlet. Place service tests under the matching domain folder and name files `*Tests.cs`, for example `CreateAccountServiceTests.cs`. Name test methods as `Method_Scenario_ExpectedResult`. Mock repository interfaces, not `AppDbContext`, and keep Arrange / Act / Assert blocks visually separated.

## Commit & Pull Request Guidelines
Follow the repository commit convention: `{type}: {Korean imperative summary}`. Common prefixes are `feat`, `fix`, `update`, `docs`, `chore`, and `cicd`. Keep commits focused on one logical change and avoid mixing feature work with unrelated fixes. PRs should explain the behavior change, list validation steps such as `dotnet test Fantasy.Test`, reference the related issue, and include request/response samples when an API contract changes.

## Security & Configuration Tips
Do not commit real secrets. Use `Fantasy.Server/appsettings.Development.json` only for local defaults, and override database, Redis, and JWT settings through environment variables or compose files for shared environments.
