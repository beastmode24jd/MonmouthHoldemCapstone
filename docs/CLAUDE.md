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
| `IAuthenticationService` | `AuthenticationService` | Login, register, logout, password reset (token-based), email confirmation |
| `IUserService` | `UserService` | Profile management, deactivation |
| `IProfileImageService` | `ProfileImageService` | Upload/retrieve profile images |
| `ISightingsService` | `SightingsService` | Submit and query wildlife sightings. `GetAllSightingsAsync()` eager-loads `User` nav property for attribution; `GetUserSightingsAsync(Guid)` filters to one user (no include). |
| `IScoringService` | `ScoringService` | Award points using rarity multiplier |
| `IBadgeService` | `BadgeService` | Check and award badges |
| `ILeaderboardService` | `LeaderboardService` | Ranked user standings |
| `IReportService` | `ReportService` | Submit and resolve content reports |
| `INotificationService` | `InAppNotificationService` | Create and deliver in-app notifications |
| `IEmailService` | `AzureCommunicationEmailService` / `NoOpEmailService` | Send emails (toggled by `UseRealEmailerService` feature flag). Used by `AccountController.ForgotPassword` to deliver password-reset links. |
| `IApiCaller` | `ExternalApiCaller` | HTTP calls to external APIs with SQL caching |
| `IClubService` | `ClubService` | List public clubs, list user's clubs, create a club (auto-enrolls owner as first member) |

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
| `ClubsController` | Club listing, creation (POST with notification + timezone); club page and chatroom (stubs) |

---

## Feature Flags

`FeatureFlags` is registered as a singleton from `appsettings.json` configuration. Current flags:

- `UseRealEmailerService` — when `true`, uses `AzureCommunicationEmailService`; otherwise `NoOpEmailService`
- `EnableEmailTestEndpoint` — when `true`, exposes two test-only endpoints that return token links as plain text (gated, safe for test environments only). Always forced `true` by `TestWebAppHost` via in-memory config override:
  - `GET /Account/GeneratePasswordResetLink?email=xxx` — fresh password-reset URL
  - `GET /Account/GenerateEmailConfirmationLink?email=xxx` — fresh email-confirmation URL

---

## Testing Strategy

### Unit Tests

- **`Domain.Tests.Unit`** — tests each service in isolation using Moq for all dependencies. Covers: `AuthenticationService`, `BadgeService`, `LeaderboardService`, `ScoringService`, `SightingsService`, `UserService`, `ReportService`, `NotificationService`, `ExternalApiCaller`.
- **`WebApp.Tests.Unit`** — tests controllers and view models in isolation.

### Integration Tests

- **`Tests.Integration`** — uses `Microsoft.AspNetCore.Mvc.Testing` with EF Core In-Memory to test the full HTTP pipeline. Covers leaderboard, reports, and sightings gallery endpoints.

### Acceptance Tests (BDD)

- **`Tests.Acceptance`** — Reqnroll feature files in `Features/`, step definitions in `StepDefinitions/`, Selenium page objects in `PageObjects/`, and browser drivers in `Drivers/`. A `TestWebAppHost.cs` auto-starts the web application when tests run. Uses `GlobalHooks` (must be static) for Reqnroll lifecycle management.

#### Acceptance test infrastructure details

- **Environment:** `ASPNETCORE_ENVIRONMENT = "Acceptance"`. The in-process `TestWebAppHost` starts a real Kestrel listener using `appsettings.Acceptance.json` (default port `https://localhost:5001`).
- **Database:** Real SQL Server LocalDB (`WAID_AppDataDb`) — **not** InMemory. Migrations and seeding run normally on startup, same as production.
- **Config load order:** `appsettings.json` → `appsettings.Acceptance.json` → `appsettings.Acceptance.Local.json` (gitignored, per-developer overrides) → environment variables.
- **Browser:** One shared `ChromeDriver` instance for the entire test run (`BeforeTestRun` / `AfterTestRun`). No browser restart between scenarios.
- **Scenario isolation:** `TestWebAppHost.ResetSeedData()` exists as a `TODO` stub. Until implemented, scenarios must be written to tolerate persistent database state across the run — or must clean up after themselves.
- **DI in steps:** Reqnroll's per-scenario DI container (via `[ScenarioDependencies]` in `TestDependencySetup`) provides `IWebDriver` and `AcceptanceTestSettings` as singletons. Drivers and page objects are resolved automatically as transient. Every new Driver must be registered in `TestDependencySetup.CreateServices()` as `services.AddTransient<TDriver>()` — Reqnroll does not auto-discover drivers.
- **Password reset test pattern:** For scenarios that require a user to receive and click a reset link, call `PasswordResetDriver.GetPasswordResetLink(email)` (hits `GET /Account/GeneratePasswordResetLink`) to get the URL, then navigate to it. This mimics clicking the emailed link without a real inbox. `TestWebAppHost` always forces `EnableEmailTestEndpoint = true` via in-memory config override so this works in any environment without appsettings changes.
- **Email confirmation test pattern:** Same approach — call `EmailVerificationDriver.GetEmailConfirmationLink(email)` (hits `GET /Account/GenerateEmailConfirmationLink`) to get the verification URL, then navigate to it. Acceptance test scenarios that need a fresh unverified user register with a unique `csp134_{guid}@test.com` email so they remain isolated without a DB reset between scenarios.
- **Registration UX change (CSP-134):** Registration no longer auto-signs the user in. It sends a verification email and redirects to `/Account/RegisterConfirmation`. Users must click the verification link before they can log in. All seeded personas in `AcceptanceTestSeeder` have `EmailConfirmed = true` and are not affected.

#### Seed user already referenced in step definitions

`CSP53StepDefinitions` hard-codes: **`alpha@test.com` / `Capstone26!`** as "user Alpha". This user must exist in the `WAID_AppDataDb` database with the `User` role for any CSP-53 scenarios to pass.

#### Page element IDs used by existing PageObjects/Drivers

| Page | Element ID | Purpose |
|---|---|---|
| Any page | `userDropdownNavDisplay` | Detect logged-in user (nav bar) |
| Any page | `logoutBtn` | Logout button |
| `/Account/Login` | `emailField` | Username input |
| `/Account/Login` | `passwordField` | Password input |
| `/Account/Login` | `RememberMe` | Remember me checkbox |
| `/Account/Login` | `submitBtn` | Login submit button |
| `/Sighting/Create` | `Latitude` | Latitude input |
| `/Sighting/Create` | `Longitude` | Longitude input |
| `/Sighting/Create` | `Timestamp` | Timestamp input |
| `/Sighting/Create` | `Description` | Description textarea |
| `/Sighting/Create` | `UploadedImage` | Image file upload |
| `/Sighting/Create` | `SubmitBtn` | Form submit button |
| `/Account/ForgotPassword` | `forgotPasswordEmail` | Email input |
| `/Account/ForgotPassword` | `sendResetEmailBtn` | Submit button |
| `/Account/ForgotPassword` | `resetEmailSentMessage` | "Check your email" success banner (shown after submit, regardless of whether email exists) |
| `/Account/ResetPassword` | `newPasswordField` | New password input |
| `/Account/ResetPassword` | `confirmPasswordField` | Confirm password input |
| `/Account/ResetPassword` | `resetPasswordBtn` | Submit button |
| `/Account/ResetPassword` | `resetPasswordError` | Inline validation-summary div (visible when token is invalid/expired) |
| `/Account/ResetPasswordInvalid` | `invalidResetLinkMessage` | Error div on the dedicated invalid-link page |
| `/Account/ResetPasswordInvalid` | `requestNewResetLinkBtn` | Link to request a new reset link |
| `/Account/Login` | `passwordResetSuccessMessage` | Success banner shown after a completed password reset |
| `/Account/Login` | `emailNotVerifiedMessage` | Warning banner + resend button shown when unverified user tries to log in |
| `/Account/Login` | `resendVerificationBtn` | "Resend Verification Email" button in the unverified-user warning |
| `/Account/RegisterConfirmation` | `registrationConfirmationMessage` | "Check your email" success message shown after registration |
| `/Account/RegisterConfirmation` | `resendFromConfirmationLink` | Link to resend verification from the confirmation page |
| `/Account/VerifyEmail` | `emailVerifiedSuccessMessage` | Success message shown after a valid confirmation link is clicked |
| `/Account/VerifyEmail` | `loginAfterVerificationBtn` | "Log In" button on the success page |
| `/Account/VerifyEmail` | `emailVerificationErrorMessage` | Error shown when the confirmation token is invalid or expired |
| `/Account/VerifyEmail` | `requestNewVerificationBtn` | Link to request a new verification link from the error page |
| `/Account/ResendVerification` | `resendVerificationEmail` | Email input on the resend form |
| `/Account/ResendVerification` | `resendVerificationSubmitBtn` | Submit button on the resend form |
| `/Account/ResendVerification` | `resendVerificationSentMessage` | "Check your email" success banner after resend |
| `/Sighting/Gallery` | `filterAll` | "All Sightings" toggle button |
| `/Sighting/Gallery` | `filterMine` | "My Sightings" toggle button |
| `/Sighting/Gallery` | `emptyStateMine` | Empty-state div shown by JS when "My Sightings" has no results |
| `/Sighting/Gallery` | `sightingsGrid` | Container `div` holding all card wrappers (when sightings exist) |
| `/Sighting/Gallery` | `currentUserId` | Hidden `<span data-user-id="…">` carrying the logged-in user's identity string ID |
| `/Sighting/Gallery` | `.sighting-card-wrapper[data-user-id]` | Per-card wrapper; `data-user-id` attribute used by JS to match against current user |
| `/Sighting/Gallery` | `.sighting-attribution` | `<span>` inside each card showing the submitter's `UserName` |

Access-denied detection: checks if `driver.Url` contains `/account/login` (case-insensitive redirect).

---

## PBI Implementation Workflow

When implementing a Jira PBI (backlog item), **always** deliver both the feature code and its tests. "Done" means the story works and is tested.

### Test requirements per PBI

- **Unit tests** — cover all new/modified service methods and controller actions in isolation (NUnit + Moq + FluentAssertions)
- **BDD/Acceptance tests** — at least one Reqnroll `.feature` scenario per acceptance criterion in the PBI (Selenium end-to-end)

### Red / Green / Refactor

Follow Red/Green/Refactor whenever feasible:

1. **Red** — write a failing test that captures the requirement
2. **Green** — write the minimal implementation to make it pass
3. **Refactor** — clean up without breaking the test (skip if nothing needs cleaning)
4. **Commit** — one commit at the end of the full cycle

For BDD scenarios, implement **one scenario per commit** (write the feature step + step definition + implementation together as a single unit of work).

### Commit message convention for TDD cycles

```
[CSP-XXX] <what was implemented> (TDD)
[CSP-XXX] BDD: <scenario name from .feature file>
```

### Post-implementation: update this file

After completing every PBI, update `docs/CLAUDE.md` as a final committed step:

- Add any new page element IDs to the element ID table above
- Update service/interface descriptions if methods were added or changed
- Note new ViewModel properties or constructor signatures if they affect how tests are written
- Document any new acceptance test infrastructure (Drivers, PageObjects)
- Record any non-obvious patterns discovered (e.g., Moq quirks, EF eager-loading conventions)

The goal is that any developer — human or AI — can pick up the next feature without scanning the codebase from scratch. This file is only as useful as it is current.

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
| Acceptance testing guide | `docs/acceptance_testing.md` |
| Architectural guidelines | `docs/architectural_guidelines.md` |

---

## Detailed Database Schema

Two EF Core DbContexts, both using the `DataDb` connection string.

### ApplicationDbContext tables

#### `AspNetUsers` (`ApplicationUser : IdentityUser`)

| Column | Type | Notes |
|---|---|---|
| `Id` | nvarchar(450) | PK, GUID stored as string |
| `Email` | nvarchar(256) | |
| `NormalizedEmail` | nvarchar(256) | Indexed |
| `UserName` | nvarchar(256) | |
| `NormalizedUserName` | nvarchar(256) | Unique index (nullable-filtered) |
| `PasswordHash` | nvarchar(max) | |
| `EmailConfirmed` | bit | |
| `ProfileImage` | varbinary(max) | nullable; null = use default avatar |
| `ProfileImageType` | nvarchar(50) | nullable; e.g. `"image/png"` |
| `IsDeactivated` | bit | soft-delete flag |
| `Points` | int | total accumulated points |
| `Bio` | nvarchar(250) | nullable |
| `LastLogin` | datetimeoffset | nullable |
| `LoginStreak` | int | days in current streak |
| Standard Identity columns | — | `SecurityStamp`, `ConcurrencyStamp`, `LockoutEnabled`, `LockoutEnd`, `AccessFailedCount`, `TwoFactorEnabled`, `PhoneNumber`, `PhoneNumberConfirmed` |

`IsStreakActive` (not mapped): true when `(UtcNow − LastLogin) ≤ 30 days`.

#### `Sighting`

| Column | Type | Constraints |
|---|---|---|
| `Id` | uniqueidentifier | PK, identity |
| `UserId` | nvarchar(450) | FK → AspNetUsers (cascade delete), indexed |
| `Lat` | decimal(9,6) | -90 to 90 |
| `Long` | decimal(9,6) | -180 to 180 |
| `Timestamp` | datetimeoffset | must be in the past (`[PastDateTime]`) |
| `Description` | nvarchar(500) | nullable |
| `ImageBuffer` | varbinary(max) | required; 1 byte – 2 MB |

#### `Badge`

| Column | Type | Constraints |
|---|---|---|
| `BadgeID` | uniqueidentifier | PK |
| `Title` | nvarchar(50) | 1–50 chars |
| `Description` | nvarchar(150) | max 150 |
| `PointValue` | int | default 10 |
| `BadgeIcon` | varbinary(max) | nullable |

Three badges are always seeded (idempotent upsert in `ApplicationDbContextSeeding`):

| Constant | GUID | Title | Points |
|---|---|---|---|
| `BadgeId.ProfileBadgeGUID` | `A1B2C3D4-E5F6-4789-8A9B-0C1D2E3F4A5B` | Custom Profile Badge | 10 |
| `BadgeId.CustomBioBadgeGUID` | `91E7773E-F6D7-457E-911E-8246891D65A2` | Custom Bio Badge | 10 |
| `BadgeId.FirstSightingBadgeGUID` | `B2C3D4E5-F6A7-4890-9B0C-1D2E3F4B5A6F` | First Sighting Badge | 25 |

#### `PersonalBadges` (`UserBadge`)

| Column | Type | Notes |
|---|---|---|
| `UserBadgeId` | uniqueidentifier | PK |
| `User ID` | nvarchar(450) | FK → AspNetUsers (cascade), indexed |
| `Badge ID` | uniqueidentifier | FK → Badge (cascade), indexed |
| `BadgeEarned` | datetimeoffset | nullable |

#### `Notification`

| Column | Type | Constraints |
|---|---|---|
| `Id` | uniqueidentifier | PK, identity |
| `RecipientId` | nvarchar(450) | FK → AspNetUsers (cascade), indexed |
| `Title` | nvarchar(50) | 1–50 chars |
| `Message` | nvarchar(250) | 1–250 chars |
| `SentAt` | datetimeoffset | required |
| `IsRead` | bit | default false |

`IsPostdated` (not mapped): true when `SentAt > UtcNow` — future-dated delivery is supported.

#### `Report`

| Column | Type | Constraints |
|---|---|---|
| `Id` | uniqueidentifier | PK, identity |
| `ReportingUserId` | nvarchar(450) | FK → AspNetUsers (cascade) |
| `ReportedPageUrl` | nvarchar(2048) | required |
| `Reason` | nvarchar(100) | required |
| `Description` | nvarchar(1000) | nullable |
| `SubmittedAt` | datetime2 | defaults to `DateTime.UtcNow` |
| `IsResolved` | bit | default false |

Unique filtered index on `(ReportingUserId, ReportedPageUrl)` where `IsResolved = 0` — prevents duplicate open reports, but allows re-reporting after resolution.

#### `EmailQueue`

| Column | Type | Notes |
|---|---|---|
| `Id` | uniqueidentifier | PK, identity |
| `Recipient` | nvarchar(450) | email address |
| `Subject` | nvarchar(250) | |
| `HtmlBody` | nvarchar(max) | required |
| `PlainTextBody` | nvarchar(max) | nullable |
| `CreatedAt` | datetimeoffset | defaults to `UtcNow` |
| `ScheduledAt` | datetimeoffset | nullable; null = send immediately |
| `IsSent` | bit | |
| `SentAt` | datetimeoffset | nullable |
| `Attempts` | int | retry count |
| `LastAttemptAt` | datetimeoffset | nullable |
| `LastError` | nvarchar(max) | nullable |
| `Processing` | bit | dispatcher lock flag |

Composite index on `(IsSent, ScheduledAt)` for dispatcher queries.

### Standard ASP.NET Identity tables (auto-managed)

`AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`.

Seeded roles: `User`, `Admin`.

### CacheDbContext tables

`ApiCallerCacheEntity` / `NinjaAnimalCacheEntity` — SQL-backed cache for API-Ninjas Animals API responses. Managed separately; migrations in `src/MH.Capstone.Domain/Migrations/Cache/`.

### FK dependency order for seeding

```
AspNetRoles
  ↓
AspNetUsers (ApplicationUser)
  ↓
Badge          (no FK dependencies)
  ↓
Sighting       (FK → AspNetUsers)
PersonalBadges (FK → AspNetUsers + Badge)
Notification   (FK → AspNetUsers)
Report         (FK → AspNetUsers)
EmailQueue     (no FK; standalone outbox)
```

---

## Test Seed Data Guidance

### Constraints to remember when constructing test data

- `Sighting.ImageBuffer` is **required and non-empty** — use a 1-byte placeholder `new byte[] { 0x01 }` (matches existing `SightingValidValuesSource.DefaultValidSighting` pattern)
- `Sighting.Timestamp` must be **in the past** (`[PastDateTime]` attribute validates this) — use `DateTimeOffset.UtcNow.AddDays(-N)`
- `Report` unique filtered index: a user cannot have two **unresolved** reports for the same URL — stagger `IsResolved` values or use different URLs when seeding multiple reports per user
- `UserBadge` requires the `Badge` row to exist first — always seed badges before `PersonalBadges` (the three standard badges are always seeded by `ApplicationDbContextSeeding`)
- `ApplicationUser.Id` is a GUID stored as a string — use fixed GUIDs (not `Guid.NewGuid()`) in seed data so foreign keys remain stable across re-seeds
- Passwords must satisfy Identity policy: min 8 chars, requires digit, uppercase, lowercase, non-alphanumeric — e.g. `Capstone26!`
- Users need `NormalizedEmail` and `NormalizedUserName` set (`.ToUpper()`) and a hashed password via `PasswordHasher<ApplicationUser>`
- Assign users to the `User` or `Admin` role via `AspNetUserRoles` (role rows are seeded by `ApplicationDbContextSeeding`)

### Acceptance test seed personas

These users must exist in `WAID_AppDataDb` for acceptance tests to pass. The password for all test users is `Capstone26!`.

| Email | Role | Points | Badges | Sightings | Purpose |
|---|---|---|---|---|---|
| `alpha@test.com` | User | 75 | FirstSighting | 3 sightings | **Required** — hard-coded in `CSP53StepDefinitions` as "user Alpha"; used for all sighting upload scenarios |
| `alice@test.com` | User | 200 | Profile + FirstSighting | 5 sightings | Leaderboard top-ranked user; exercises badge + scoring paths |
| `bob@test.com` | User | 20 | (none) | 1 sighting | Mid-ranked user; no badges yet |
| `newuser@test.com` | User | 0 | (none) | 0 sightings | Baseline new account; exercises empty-state views |
| `admin@test.com` | Admin | 0 | (none) | 0 sightings | Admin-role user for moderation/report scenarios; also satisfies `AdminAccount:Hidden` config if set to this address |

### Suggested sighting locations (Pacific Northwest theme)

| Label | Latitude | Longitude | User | Notes |
|---|---|---|---|---|
| WOU Campus | 44.847600 | -123.234300 | alpha | Within Salem-area bounds |
| Silver Falls | 44.877000 | -122.654000 | alpha | Common — many global sightings |
| Crater Lake | 42.944600 | -122.109000 | alice | Rare/mythic — few global sightings |
| Portland | 45.523100 | -122.676200 | alice | Urban sighting |
| Eugene | 44.052100 | -123.086800 | alice | Mid-range sighting |
| Outside Oregon | 34.052200 | -118.243700 | bob | LA — useful for map bounds filtering tests |

### Scoring tier thresholds to seed around

Per `ScoringService`:
- **Mythic** (≤5 global sightings of a species): 10 pts × 5 = **50 pts**
- **Rare** (6–50 global sightings): 10 pts × 2 = **20 pts**
- **Common** (>50 global sightings): 10 pts × 1 = **10 pts**

Seed at least one sighting per tier to exercise all scoring branches.

### Notification scenarios to seed

- At least one **unread** notification for `alpha` — so the notification bell/badge has something to display
- At least one **read** notification — to verify read-state rendering
- Optionally one **postdated** notification (`SentAt > UtcNow`) to test `IsPostdated` path

### LoginStreak scenarios to seed

| User | `LastLogin` | `LoginStreak` | `IsStreakActive` |
|---|---|---|---|
| `alpha` | `UtcNow - 1 day` | 5 | true |
| `bob` | `UtcNow - 31 days` | 3 | false (expired) |
| `newuser` | null | 0 | false (never logged in) |

### Where to add acceptance seed data

The acceptance-specific seed users should be added to `ApplicationDbContextSeeding.SeedDataAsync` gated on an environment check, **or** in a dedicated acceptance-only seeding method called from `TestWebAppHost.StartAsync`. The latter is preferred so production/staging seeding remains unaffected.

`TestWebAppHost.ResetSeedData()` is currently a `NotImplementedException` stub — implementing it to truncate non-badge rows and re-run seed will be required for true scenario isolation once test count grows.

---

## Clubs Feature (Sprint 4 — in progress)

### Overview

Clubs are groups users can create and join. A club can be **public** (visible to all authenticated users) or **private** (visible only to members). The owner is automatically enrolled as the first member on creation. Each club has a chatroom (`Message` table) for member communication.

> **IMPORTANT:** `ClubService` has a pending constraint: deleting a user will throw if they still have club memberships or messages. Any future user-deletion logic must clean up `ClubMembership` and `Message` rows first.

---

### New Entities

All in `src/MH.Capstone.Domain/DataModels/`:

#### `Club` (table: `Club`)

| Column | Type | Constraints |
|---|---|---|
| `Id` | uniqueidentifier | PK, identity |
| `Name` | nvarchar(100) | required, 1–100 chars |
| `IsPublic` | bit | required; `false` = private |
| `Description` | nvarchar(250) | nullable |
| `CreatedAt` | datetimeoffset | required |
| `OwnerId` | nvarchar(450) | FK → AspNetUsers; mapped column for `OwnerIdentityId` |

Nav properties: `Owner` (ApplicationUser), `Memberships` (List\<ClubMembership\>), `Messages` (List\<Message\>).

`OwnerId` / `OwnerIdentityId` pattern: `OwnerId` is a `[NotMapped]` `Guid` convenience property; `OwnerIdentityId` is the actual `string` column (same pattern as `ApplicationUser.GuidId`).

Constructors: `Club()` (default) and `Club(Guid ownerId, string name, string? description, DateTimeOffset createdAt)`.

#### `ClubMembership` (table: `ClubMembership`)

| Column | Type | Constraints |
|---|---|---|
| `Id` | uniqueidentifier | PK, identity |
| `MemberId` | nvarchar(450) | FK → AspNetUsers |
| `ClubId` | uniqueidentifier | FK → Club |
| `JoinedAt` | datetimeoffset | required |

`MemberId` / `MemberIdentityId` follow the same `[NotMapped]` Guid / mapped string column pattern as `Club.OwnerId`.

Constructors: `ClubMembership()` (default) and `ClubMembership(Guid memberId, Guid clubId, DateTimeOffset joinedAt)`.

#### `Message` (table: `Message`)

| Column | Type | Constraints |
|---|---|---|
| `Id` | uniqueidentifier | PK, identity |
| `ClubId` | uniqueidentifier | FK → Club |
| `AuthorId` | nvarchar(450) | FK → AspNetUsers |
| `Content` | nvarchar(2000) | required, 1–2000 chars |
| `SentAt` | datetimeoffset | required |

`AuthorId` / `AuthorIdentityId` follow the same pattern.

Constructors: `Message()` (default) and `Message(Guid clubId, Guid authorId, string content, DateTimeOffset sentAt)`.

**Migration:** `20260425000357_AddClubsAndMessageTables`

---

### Service

`IClubService` / `ClubService` — `src/MH.Capstone.Domain/Services/`

| Method | Behaviour |
|---|---|
| `GetPublicClubsAsync()` | Returns all clubs where `IsPublic = true` |
| `GetUserClubsAsync(Guid userId)` | Returns clubs the user has a `ClubMembership` row for, sorted by `Name` |
| `GetClubByIdAsync(Guid id)` | Eagerly loads all clubs with `Owner` included via `GetAllAsync(c => c.Owner)`, then returns the first matching `Id`. Returns `null` if not found. |
| `CreateClubAsync(Club club)` | Saves the club, then auto-enrolls the owner as the first `ClubMembership`; throws `ArgumentNullException` on null |
| `SendInviteAsync` / `AcceptInviteAsync` / `DeclineInviteAsync` | Stub methods — interface defined, implementations empty |

---

### Controller

`ClubsController` (`[Authorize]`) — `src/MH.Capstone.WebApp/Controllers/ClubsController.cs`

Injects: `IClubService`, `UserManager<ApplicationUser>`, `INotificationService`, `ILogger<ClubsController>`.

| Action | Route | Status |
|---|---|---|
| `Index()` | `GET /Clubs` | **Done** — loads `ClubListViewModel`, renders `LandingPage` view |
| `ClubPage(Guid id)` | `GET /Clubs/ClubPage/{id}` | **Done** — fetches club via `GetClubByIdAsync` (Owner eagerly loaded); checks membership via `GetUserClubsAsync`; returns 404 if club not found, 403 if private and non-member; renders `ClubPage` view with `ClubPageViewModel` |
| `Chatroom(Guid id)` | `GET /Clubs/Chatroom/{id}` | **Stub** — returns the `Chatroom` view; no data passed yet |
| `CreateClub(string name, string? description, bool isPublic)` | `POST /Clubs/CreateClub` | **Done** — saves club via `CreateClubAsync`; sends an in-app notification to the owner; redirects to `GET /Clubs/ClubPage/{id}` |

---

### ViewModels

`ClubListViewModel` — `src/MH.Capstone.WebApp/Models/ClubListViewModel.cs`

| Property | Type | Notes |
|---|---|---|
| `PublicClubs` | `List<Club>` | All public clubs |
| `UserClubs` | `List<Club>` | Clubs the current user is a member of |
| `CurrentUserId` | `string` | Identity string ID of the logged-in user |
| `HasPublicClubs` | bool | Computed |
| `HasPersonalClubs` | bool | Computed |
| `PublicClubCount` / `UserClubCount` | int | Computed |

`ClubPageViewModel` — `src/MH.Capstone.WebApp/Models/ClubPageViewModel.cs`

| Property | Type | Notes |
|---|---|---|
| `Club` | `Club` | The club entity; `Owner` nav property is eagerly loaded by `GetClubByIdAsync` |
| `IsCurrentUserOwner` | `bool` | True when the logged-in user's `GuidId` matches `Club.OwnerId` |
| `IsCurrentUserMember` | `bool` | True when the club appears in the user's `GetUserClubsAsync` result |

Constructor: `ClubPageViewModel(Club club, bool isOwner, bool isMember)`.

---

### Views

| View | Path | Status |
|---|---|---|
| `LandingPage.cshtml` | `Views/Clubs/LandingPage.cshtml` | Done — filter UI, club cards grid (public + private user clubs), "View Club" links on each card, create-club modal |
| `ClubPage.cshtml` | `Views/Clubs/ClubPage.cshtml` | Done — club name, visibility badge, description, owner username, created date, "Go to Chatroom" button, "Invite Member" button + modal (owner-only; send not yet implemented), "Back to Clubs" link |
| `Chatroom.cshtml` | `Views/Clubs/Chatroom.cshtml` | Stub — wired to `GET /Clubs/Chatroom/{id}`, no content yet |

---

### Page Element IDs — `/Clubs/ClubPage/{id}` (ClubPage)

| Element ID | Purpose |
|---|---|
| `inviteMemberBtn` | "Invite Member" button; only rendered for the club owner; opens `inviteMemberModal` |
| `inviteMemberModal` | Bootstrap modal for invite flow; only rendered for the club owner |
| `memberSearchInput` | Username search input inside the invite modal |
| `memberSearchResults` | `<div>` where search results will be injected (not yet implemented) |
| `sendInviteBtn` | "Send Invite" button inside the modal; disabled until invite feature is implemented |

---

### Page Element IDs — `/Clubs` (LandingPage)

| Element ID | Purpose |
|---|---|
| `filterAll` | "All Public Clubs" toggle button |
| `filterMine` | "My Clubs" toggle button |
| `clubCountLabel` | Visible count label (updated by JS) |
| `emptyStateAll` | Server-rendered empty state when no public clubs exist at all |
| `emptyStateMine` | JS-toggled empty state when user has no club memberships |
| `clubsGrid` | Grid container `div` holding all `.club-card-wrapper` elements |
| `.club-card-wrapper[data-user-id]` | Per-card wrapper; `data-user-id` = `OwnerIdentityId` string |
| `currentUserId` | Hidden `<span data-user-id="…">` carrying the current user's identity string ID |
| `newClubModal` | Bootstrap modal for the "Create a new Club" form |
| `modalClubName` | Club name text input inside the modal |
| `descInput` | Club description textarea inside the modal (max 250 chars) |
| `charCount` | Live character count display (`0/250`) |
| `descErrorMsg` | Inline validation error div inside the modal |
| `confirmAuthBtn` | `type="submit"` button inside the modal — submits the form to `POST /Clubs/CreateClub` |

Filter state is persisted with `sessionStorage` key `'clubsFilter'` (`'all'` or `'mine'`).

---

### Unit Tests

`ClubServiceTests` — `src/MH.Capstone.Domain.Tests.Unit/Services/ClubServiceTests.cs`

| Test | Covers |
|---|---|
| `GetPublicClubsAsync_ReturnsOnlyPublicClubs` | Filters out private clubs |
| `GetUserClubsAsync_ReturnsOnlyUserClubs_SortedByClubName` | Returns only clubs the user has a membership for |
| `CreateClubAsync_ValidClub_SavesClubAndOwnerMembershipReturnsClub` | Happy path: persists club and owner membership |
| `CreateClubAsync_NullClub_ThrowsArgumentNullException` | Null guard |

---

### What Is Still Incomplete

- `Chatroom.cshtml` is a stub — the GET route exists (`/Clubs/Chatroom/{id}`) but the view has no content, and there is no service method for messages yet
- Invite feature: `memberSearchInput` in `ClubPage.cshtml` is non-functional; `SendInviteAsync` / `AcceptInviteAsync` / `DeclineInviteAsync` in `ClubService` are empty stubs
- No acceptance tests (`.feature` files) exist yet for any Club scenarios
- User-deletion flow must be updated to clean up `ClubMembership` and `Message` rows before removing a user

---

## Jira PBI / User Story Guidelines

> The human-readable version of these guidelines lives at `docs/pbi_guidelines.md`.

This section governs how AI assistants (and developers) should create or update Jira issues in the CSP project. All user stories must conform to the **INVEST** principles and follow the established story structure below.

---

### INVEST Principles (required for every story)

Every user story written for this project must satisfy all six INVEST criteria before being submitted to Jira:

| Principle | Requirement |
|---|---|
| **Independent** | The story must be self-contained with no inherent dependency on another story. |
| **Negotiable** | Until a story enters an active sprint, it can always be rewritten or changed. |
| **Valuable** | The story must deliver clear value to the end user. |
| **Estimable** | The story must be scoped clearly enough that the team can estimate its size. |
| **Small** | The story must be small enough to plan, task, and prioritize with certainty. |
| **Testable** | The story must provide enough detail for test development to be possible. |

---

### User Story Structure

Every Jira issue must include the following sections. Use the template below exactly — do not omit sections.

#### Story Case (Summary / Title field)

Write in the standard user story format:

```
As a <role>, when <context>, I want <goal> so that <benefit>.
```

#### Description

Provide 2–4 sentences of background explaining the current state and what this story changes. Follow with a bulleted list of specific behavioral requirements the implementation must satisfy.

#### Assumptions / Preconditions

Organize assumptions into four subsections:

- **Functional Assumptions** — what the system already provides that this story depends on
- **Security Assumptions** — authentication, authorization, and data visibility rules
- **User Experience Assumptions** — UI behavior, empty states, transitions, labeling
- **System Behavior Assumptions** — backend/data layer behavior, performance, pagination

#### Acceptance Criteria

Write all acceptance criteria as Gherkin scenarios using `Given / When / Then` format, wrapped in a fenced Gherkin code block:

````
```Gherkin
Scenario: <scenario name>
    Given <precondition>
    When <action>
    Then <expected outcome>
    And <additional assertion>
```
````

Each acceptance criterion from the description must map to at least one scenario. Cover: happy path, alternative paths, empty states, and any security/visibility rules.

---

### Example Story (reference)

The following is a canonical example of a well-formed story for this project:

**Story Case:**
> As a User, when I visit the gallery page, I want to view sightings submitted by all users so that I can explore the broader community's observations, while still being able to filter the gallery to show only my own sightings when I choose.

**Description:**
Currently, the gallery page displays only the authenticated user's own sightings. This story expands the gallery to show sightings from all users by default, turning it into a community-wide feed. Users retain the ability to filter the gallery down to only their own submissions at any time.

Requirements:
- Display all sightings from all users by default, sorted by most recent
- Show relevant attribution on each sighting card (e.g., submitted by username or display name)
- Provide a filter control (toggle or dropdown) allowing the user to switch between "All Sightings" and "My Sightings"
- Persist the selected filter for the duration of the session (or until changed)
- Respect existing visibility/privacy rules — private sightings must not appear in the community view

**Acceptance Criteria (Gherkin):**
```Gherkin
Scenario: Default gallery shows all community sightings
    Given an authenticated user navigates to the gallery page
    When the page loads with no filter selected
    Then sightings from all users are displayed
    And each sighting card shows the submitting user's attribution

Scenario: User filters gallery to their own sightings
    Given an authenticated user is on the gallery page
    When the user selects the "My Sightings" filter
    Then only sightings submitted by the authenticated user are displayed
    And the filter control reflects the active "My Sightings" state

Scenario: User clears the filter to return to community view
    Given an authenticated user has the "My Sightings" filter active
    When the user selects the "All Sightings" filter
    Then sightings from all users are displayed again
    And the filter control reflects the active "All Sightings" state

Scenario: Private sightings are excluded from community view
    Given a user has submitted a sighting marked as private
    When any other user views the gallery in "All Sightings" mode
    Then the private sighting is not visible to them

Scenario: Empty state when user has no sightings
    Given an authenticated user has not submitted any sightings
    When the user selects the "My Sightings" filter
    Then an empty state message is displayed
    And the user is prompted to submit their first sighting

Scenario: Filter persists within the session
    Given an authenticated user has selected the "My Sightings" filter
    When the user navigates away and returns to the gallery page within the same session
    Then the "My Sightings" filter remains active
```

---

### Required Jira Fields

In addition to the story content, every Jira issue must have the following fields set:

#### Team
Always assign the team **"MH Development Team"** unless explicitly told otherwise.

#### Story Point Estimate (SPE)
Use powers of 2 only: **1, 2, 4, 8, …**. Target ≤ 4 points per story — if a story feels larger, consider splitting it.

| Points | When to use |
|---|---|
| **1** | Minor bug fix; UI-only update; no new testing or back-end code, or only minimal/routine changes to existing tests and back-end. |
| **2** | Larger full-stack bug fix; larger UI-only or back-end-only update or new design; little to moderate testing updates or implementation. |
| **4** | New full-stack feature; heavy back-end work; requires new or large overhauls of all test types (unit, acceptance, etc.). |

> Estimates may vary depending on whether existing infrastructure or prior experience is available to support the story's implementation. Use the table as a guide, not a rule — the same work can reasonably land at a different point value given context.

---

### AI Agent Attribution

When an AI agent creates or modifies a Jira PBI description, it must append the following note at the very bottom of the description field:

```
---
AI Agent <Agent Name> assisted in the creation and/or modification of this PBI.
```

Replace `<Agent Name>` with the name of the AI agent or model used (e.g., `Claude Sonnet 4.6`).

---

### Checklist before creating or updating a Jira issue

- [ ] Story case follows `As a / when / I want / so that` format
- [ ] All six INVEST criteria are satisfied
- [ ] Description includes background context and a bulleted requirements list
- [ ] Assumptions are organized into the four subsections
- [ ] Every requirement maps to at least one Gherkin scenario
- [ ] Gherkin scenarios cover happy path, alternative paths, empty states, and security rules
- [ ] Story is small enough to be completed within a single sprint
- [ ] Team is set to **MH Development Team**
- [ ] Story point estimate is set (1, 2, or 4) using the SPE guidelines above
- [ ] AI agent attribution note appended to the bottom of the description (if created or modified by an AI agent)
