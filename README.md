# Customer Directory

A CRUD web app for managing customer contact records, built with ASP.NET Core MVC, EF Core, and AJAX.

## Tech stack
- .NET 8, ASP.NET Core MVC
- EF Core 8 with SQLite
- Serilog (console + rolling file sink)
- Bootstrap 5, vanilla Fetch API (no JS framework)
- xUnit

## Prerequisites
- .NET 8 SDK
- (Optional) `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`

## Setup
```bash
git clone https://github.com/HasiruAluthge/CustomerDirectory.git
cd customer-directory
dotnet restore
dotnet ef database update --project CustomerDirectory.Infrastructure --startup-project CustomerDirectory.Web
dotnet run --project CustomerDirectory.Web
```
Navigate to the URL shown in the console (e.g. https://localhost:xxxx). The database is created and seeded with 25 customers automatically on first run in Development.

## Running tests
```bash
dotnet test
```

## Architecture
- `CustomerDirectory.Web` – controllers, Razor views, static JS, startup/config.
- `CustomerDirectory.Application` – DTOs, domain model, service interfaces (no EF Core dependency).
- `CustomerDirectory.Infrastructure` – EF Core DbContext, migrations, service implementations.
- `CustomerDirectory.Tests` – xUnit unit tests against the service layer + validation tests.

## Design decisions & assumptions
- SQLite chosen for zero-setup review, per the assignment's guidance.
- Duplicate email uniqueness enforced case-insensitively in the service layer (SQLite's default collation is case-sensitive, so the DB unique index alone isn't sufficient); the DB index remains as a backstop.
- Customer numbers generated as `CUS-#####` from a simple count-based counter — documented race condition under concurrent creates; acceptable for this scope.
- [Add anything else you decided along the way.]

## Known limitations / what I'd improve with more time
- No optimistic concurrency token yet (stretch goal).
- Customer-number generation isn't safe under high concurrency.