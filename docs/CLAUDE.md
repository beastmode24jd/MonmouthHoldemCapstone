# CLAUDE.md — Project Context for AI Assistants

This file provides a structured overview of the **Competitive Wildlife Scavenger App (CWSA)** capstone project for use by AI coding assistants. It covers purpose, architecture, conventions, and how to navigate the codebase effectively.

---

## Project Purpose

**CWSA** is a WOU Computer Science senior capstone project (team: "Monmouth Hold'em", class of 2026). The concept is "Pokemon GO meets iNaturalist with real competition."

Users go into nature, submit wildlife sightings (GPS + photo), earn points based on species rarity, climb a leaderboard, and earn achievement badges. Admins moderate content via a reports system. An Anidex species catalog (backed by the API-Ninjas Animals API) enriches sighting data.

---

## Solution Structure

```
/src
  MH.Capstone.Domain/               # Business logic, EF Core, services, repositories
  MH.Capstone.WebApp/               # ASP.NET Core MVC presentation layer
  MH.Capstone.Domain.Tests.Unit/    # Unit tests for domain services
  MH.Capstone.WebApp.Tests.Unit/    # Unit tests for controllers and view models
  MH.Capstone.Tests.Integration/    # Full HTTP pipeline integration tests
  MH.Capstone.Tests.Acceptance/     # BDD acceptance tests (Reqnroll + Selenium)
  MH.Capstone.Tests.SharedInternals/# Shared test fixtures and helpers
/docs                               # Project documentation
/.github/workflows                  # CI/CD GitHub Actions
```

---

## Technology Stack

| Category | Technology |
|---|---|
| Runtime | .NET 9 (SDK 9.0.309) |
| Web Framework | ASP.NET Core MVC |
| ORM | Entity Framework Core 9 + SQL Server |
| Authentication | ASP.NET Core Identity |
| Frontend | Bootstrap 5.3, Vanilla JS, Leaflet.js (maps via OpenStreetMap) |
| Email | Azure Communication Services (`AzureCommunicationEmailService`); `NoOpEmailService` in dev/staging |
| External API | API-Ninjas Animals API — wrapped by `ExternalApiCaller` with SQL-backed cache |
| Cloud Hosting | Azure App Service + Azure SQL |
| Unit Tests | NUnit 4, Moq, FluentAssertions, coverlet |
| Acceptance/BDD | Reqnroll (SpecFlow successor) + NUnit + Selenium ChromeDriver |
| Integration Tests | `Microsoft.AspNetCore.Mvc.Testing` + EF Core In-Memory |
| CI/CD | GitHub Actions |

---

## Architecture

The solution is a **monolithic ASP.NET Core MVC application** split into two source projects:

- **`MH.Capstone.Domain`** — all business logic: EF Core entities, two `DbContext`s, generic repository, service layer, migrations, constants, and tools.
- **`MH.Capstone.WebApp`** — presentation only: controllers, Razor views, view models, tag helpers. Wires up DI in `Program.cs`.

### Layering

```
Controller (WebApp)
    ↓ injects
Service Interface (Domain/Services/Abstraction/)
    ↓ implemented by
Service (Domain/Services/)
    ↓ injects
IRepository<T, TContext> (Domain/DataAccess/Repositories/)
    ↓
EF Core DbContext → SQL Server
```

### DbContexts

Two separate EF Core DbContexts sharing the same database connection string (`DataDb`), with separate migrations history tables:

- **`ApplicationDbContext`** (`__EFMigrationsHistory_ApplicationDbContext`) — all app data + Identity. Uses lazy loading proxies. Seeded via `ApplicationDbContextSeeding`.
- **`CacheDbContext`** — API response cache entities (`ApiCallerCacheEntity`, `NinjaAnimalCacheEntity`).

---

## Key Domain Entities

All in `src/MH.Capstone.Domain/DataModels/`:

| Entity | Key Fields |
|---|---|
| `ApplicationUser` | Extends `IdentityUser`. Custom: `Points`, `Bio`, `ProfileImage` (byte[]), `IsDeactivated`, `LastLogin`, `LoginStreak`, `IsStreakActive` |
| `Sighting` | `Latitude`/`Longitude` (DECIMAL 9,6), `Timestamp` (DateTimeOffset), `Description`, `ImageBuffer` (byte[], ≤2 MB), FK to `ApplicationUser` |
| `Badge` | `Title`, `Description`, `PointValue` (default 10), `BadgeIcon` (byte[]) |
| `UserBadge` | Join table: user ↔ badge + `AwardedAt` timestamp |
| `Notification` | `Title`, `Message`, `SentAt`, `IsRead`, `IsPostdated` (future-dated delivery support) |
| `Report` | `ReportedPageUrl`, `Reason`, `Description`, `IsResolved`. Filtered unique index: no duplicate open reports per user+URL |
| `EmailQueue` | Outbox pattern: `Recipient`, `Subject`, `HtmlBody`, `ScheduledAt`, `IsSent`, `Attempts`, `Processing` |
| `ApiCallerCacheEntity` / `NinjaAnimalCacheEntity` | SQL-backed cache for external API responses |

---

## Services

All service interfaces live in `src/MH.Capstone.Domain/Services/Abstraction/`:

| Interface | Implementation | Purpose |
|---|---|---|
| `IAuthenticationService` | `AuthenticationService` | Login, register, logout |
| `IUserService` | `UserService` | Profile management, deactivation |
| `IProfileImageService` | `ProfileImageService` | Upload/retrieve profile images |
| `ISightingsService` | `SightingsService` | Submit and query wildlife sightings |
| `IScoringService` | `ScoringService` | Award points using rarity multiplier |
| `IBadgeService` | `BadgeService` | Check and award badges |
| `ILeaderboardService` | `LeaderboardService` | Ranked user standings |
| `IReportService` | `ReportService` | Submit and resolve content reports |
| `INotificationService` | `InAppNotificationService` | Create and deliver in-app notifications |
| `IEmailService` | `AzureCommunicationEmailService` / `NoOpEmailService` | Send emails (toggled by `UseRealEmailerService` feature flag) |
| `IApiCaller` | `ExternalApiCaller` | HTTP calls to external APIs with SQL caching |

**Background service:** `EmailDispatcherService` (hosted service) processes the `EmailQueue` outbox.

---

## Scoring Logic

`ScoringService` awards points per sighting submission:

- **Base:** 10 points
- **Multiplier** based on total global sightings of that species:
  - Mythic (≤5 sightings): **5×**
  - Rare (≤50 sightings): **2×**
  - Common (>50 sightings): **1×**

---

## Controllers (WebApp)

| Controller | Responsibility |
|---|---|
| `AccountController` | Register, login, logout, profile |
| `AdminController` | Admin-only views |
| `DashboardController` | User dashboard (points, badges, recent activity) |
| `HomeController` | Landing / marketing pages |
| `LeaderboardController` | Global rankings |
| `MapController` | GPS sighting map (Leaflet.js) |
| `ReportControllers` | Submit and view content reports |
| `SightingController` | Submit and view wildlife sightings |
| `SpeciesController` | Anidex species catalog (Ninja API backed) |

---

## Feature Flags

`FeatureFlags` is registered as a singleton from `appsettings.json` configuration. Current flags:

- `UseRealEmailerService` — when `true`, uses `AzureCommunicationEmailService`; otherwise `NoOpEmailService`

---

## Testing Strategy

### Unit Tests

- **`Domain.Tests.Unit`** — tests each service in isolation using Moq for all dependencies. Covers: `AuthenticationService`, `BadgeService`, `LeaderboardService`, `ScoringService`, `SightingsService`, `UserService`, `ReportService`, `NotificationService`, `ExternalApiCaller`.
- **`WebApp.Tests.Unit`** — tests controllers and view models in isolation.

### Integration Tests

- **`Tests.Integration`** — uses `Microsoft.AspNetCore.Mvc.Testing` with EF Core In-Memory to test the full HTTP pipeline. Covers leaderboard, reports, and sightings gallery endpoints.

### Acceptance Tests (BDD)

- **`Tests.Acceptance`** — Reqnroll feature files in `Features/`, step definitions in `StepDefinitions/`, Selenium page objects in `PageObjects/`, and browser drivers in `Drivers/`. A `TestWebAppHost.cs` auto-starts the web application when tests run. Uses `GlobalHooks` (must be static) for Reqnroll lifecycle management.

---

## CI/CD

Two GitHub Actions workflows in `.github/workflows/`:

**`build_test_ci.yml`** (runs on every PR):
1. `validate-ef` job — checks both DbContexts have no pending migrations
2. `buildtest` job — restore, build (Release), `dotnet test` all projects, publish WebApp
3. When called with `deploy: true`: bundles EF migration executables for both contexts and uploads as artifact

**`deploy.yml`** (triggered on push to `main` or `dev`):
- Calls `build_test_ci.yml` with `deploy: true`
- `main` → production Azure App Service + non-prerelease GitHub Release
- `dev` → staging Azure App Service + prerelease GitHub Release
- After deploy: runs EF migration bundles against Azure SQL using OIDC passwordless auth
- Build versioning: `YYYY.M.<run_number>.<run_attempt>`

---

## Configuration Notes

- Connection string name: `DataDb` (used for both `ApplicationDbContext` and `CacheDbContext`)
- Azure Communication Services string: `ConnectionStrings:AzureCommunicationServices`
- Email sender address: `Email:SenderAddress`
- Password policy: min 8 chars, requires digit, upper, lower, non-alphanumeric
- Email confirmation: disabled for MVP (`RequireConfirmedEmail = false`)
- Cookie login path: `/Account/Login`; access denied: `/Account/AccessDenied`

---

## Key File Locations

| What | Where |
|---|---|
| DI registration / app bootstrap | `src/MH.Capstone.WebApp/Program.cs` |
| EF seeding | `src/MH.Capstone.Domain/DataAccess/ApplicationDbContextSeeding.cs` |
| EF migrations (app) | `src/MH.Capstone.Domain/Migrations/` |
| Badge GUIDs / constants | `src/MH.Capstone.Domain/Constants/BadgeId.cs` |
| Feature flags | `src/MH.Capstone.Domain/Tools/FeatureFlags.cs` |
| API Ninja contract | `src/MH.Capstone.Domain/ApiContracts/Ninja/` |
| Acceptance test features | `src/MH.Capstone.Tests.Acceptance/Features/` |
| Architectural guidelines | `docs/architectural_guidelines.md` |
