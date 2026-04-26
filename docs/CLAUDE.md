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
| `ILeaderboardService` | `LeaderboardService` | Ranked user standings. `GetLeaderboardPageAsync()` returns `IEnumerable<ApplicationUser>`; controller projects to `LeaderboardEntryViewModel` (CSP-170: excludes `Email` from public view model). |
| `IReportService` | `ReportService` | Submit and resolve content reports |
| `INotificationService` | `InAppNotificationService` | Create and deliver in-app notifications. Includes `MarkAllAsReadAsync(user)` and `DeleteAllAsync(user)` for bulk operations (implemented in `NotificationServiceBase`). |
| `IUserProfileService` | `UserService` | Extended (CSP-168) with `UpdateDisplayNameAsync(user, displayName)` — validates 2–50 chars, throws `ArgumentOutOfRangeException` if invalid. |
| `IEmailService` | `AzureCommunicationEmailService` / `NoOpEmailService` | Send emails (toggled by `UseRealEmailerService` feature flag). Used by `AccountController.ForgotPassword` to deliver password-reset links. |
| `IApiCaller` | `ExternalApiCaller` | HTTP calls to external APIs with SQL caching |

**Background service:** `EmailDispatcherService` (hosted service) processes the `EmailQueue` outbox.

**Bulk notification endpoints (CSP-138):** `PUT /notifications/mark-all-read` and `DELETE /notifications/all` — both require `[ValidateAntiForgeryToken]` and are scoped to the authenticated user.

**Display name endpoints (CSP-168):**
- `GET/POST /account/SetDisplayName` — forced setup page for users with `DisplayName == "UNSET"`; `[Authorize]`, no ANTIFORGERY needed on GET
- `POST /dashboard/UpdateDisplayName` — updates display name from dashboard settings; `[Authorize]`

**RequireDisplayNameFilter:** Global `IAsyncActionFilter` that redirects authenticated users with `DisplayName == "UNSET"` to `Account/SetDisplayName` before any other action executes. Exempted actions: `SetDisplayName`, `Login`, `Logout`, `Register`, `RegisterConfirmation`, `VerifyEmail`, `ResendVerification`, `ForgotPassword`, `ResetPassword`, `Reactivate`, `Deactivate`, and the test-only email endpoints.

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
| Any page | `userDropdownNavDisplay` | Detect logged-in user (nav bar) — contains profile image, `navDisplayNameText` span, and notification badge; use `navDisplayNameText` to read the display name in isolation |
| Any page | `navDisplayNameText` | `<span>` inside `userDropdownNavDisplay` containing only the user's display name text (excludes notification badge count) |
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
| `/Sighting/Gallery` | `.sighting-attribution` | `<span>` inside each card showing the submitter's `DisplayName` (CSP-170: no email or UserName fallback) |
| `/Account/Register` | `displayNameField` | Display name text input on the registration form |
| `/Account/SetDisplayName` | `setDisplayNameField` | Display name text input on the forced setup page |
| `/Account/SetDisplayName` | `setDisplayNameBtn` | Submit button on the forced setup page |
| `/dashboard` | `displayNameInput` | Display name text input in the Account Settings section |
| `/dashboard` | `updateDisplayNameBtn` | "Update Display Name" submit button |
| `/dashboard` | `displayNameSuccessMessage` | Success banner shown after a display name is updated |
| `/notifications` | `markAllReadForm` | Form wrapping the "Mark All as Read" button; has `d-none` class when no unread notifications exist |
| `/notifications` | `markAllReadBtn` | "Mark All as Read" submit button |
| `/notifications` | `deleteAllForm` | Form wrapping the "Delete All" button; has `d-none` class when notification list is empty |
| `/notifications` | `deleteAllBtn` | "Delete All" submit button |
| `/notifications` | `notificationsEmptyState` | `div.alert` shown when the user has no notifications |

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

### Pull Request Conventions

Every PR on this repo must follow these conventions — apply them whenever running `gh pr create`:

- **Reviewer:** always request `jmcshane22` (`--reviewer jmcshane22`)
- **Assignees:** always assign both `jmcshane22` and `beastmode24jd` (`--assignee jmcshane22,beastmode24jd`)
- **Draft:** always open as a draft (`--draft`) — PRs must not be auto-ready for merge
- **Labels:** apply relevant labels (e.g. `feature`, `bug`, `test`, `docs`) based on PR content; check available labels with `gh label list --repo jmcshane22/MonmouthHoldemCapstone`

#### `gh pr edit` is broken on this repo

`gh pr edit` exits with a GraphQL error due to the GitHub classic Projects API deprecation. Use the REST API directly instead:

```bash
# Add reviewer
gh api repos/jmcshane22/MonmouthHoldemCapstone/pulls/{n}/requested_reviewers \
  --method POST --field 'reviewers[]=jmcshane22'

# Add assignees
gh api repos/jmcshane22/MonmouthHoldemCapstone/issues/{n}/assignees \
  --method POST --field 'assignees[]=jmcshane22' --field 'assignees[]=beastmode24jd'

# Add label
gh api repos/jmcshane22/MonmouthHoldemCapstone/issues/{n}/labels \
  --method POST --field 'labels[]=enhancement'

# Convert back to draft (if created without --draft by mistake)
gh pr ready {n} --repo jmcshane22/MonmouthHoldemCapstone --undo
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

## Database Schema

Full column details are in the EF entity classes under `src/MH.Capstone.Domain/DataModels/`. Non-obvious constraints:

- `ApplicationUser.DisplayName`: `nvarchar(50)`, required, defaults to `"UNSET"` for migrated rows; 2–50 chars; `IsStreakActive` is a computed (not-mapped) property: true when `(UtcNow − LastLogin) ≤ 30 days`.
- `Sighting.ImageBuffer`: `varbinary(max)`, required (1 byte – 2 MB); `Timestamp` must be in the past (`[PastDateTime]`).
- `Report`: unique filtered index on `(ReportingUserId, ReportedPageUrl)` where `IsResolved = 0` — prevents duplicate open reports, allows re-reporting after resolution.
- `EmailQueue`: composite index on `(IsSent, ScheduledAt)`; `Processing` bit = dispatcher lock flag.
- Seeded roles: `User`, `Admin`. Three badges always seeded (idempotent upsert):

| Constant | GUID | Title | Points |
|---|---|---|---|
| `BadgeId.ProfileBadgeGUID` | `A1B2C3D4-E5F6-4789-8A9B-0C1D2E3F4A5B` | Custom Profile Badge | 10 |
| `BadgeId.CustomBioBadgeGUID` | `91E7773E-F6D7-457E-911E-8246891D65A2` | Custom Bio Badge | 10 |
| `BadgeId.FirstSightingBadgeGUID` | `B2C3D4E5-F6A7-4890-9B0C-1D2E3F4B5A6F` | First Sighting Badge | 25 |

FK seeding order: `AspNetRoles` → `AspNetUsers` → `Badge` → `Sighting` / `PersonalBadges` / `Notification` / `Report` → `EmailQueue` (no FK).

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

> **Note:** The active acceptance test seeder (`AcceptanceTestSeeder.cs`) uses different personas: **Alex** (`alex@test.com`), **Patricia** (`patricia@test.com`), **Lily** (`lily@test.com`), and **Owen** (`owen@test.com` — `DisplayName = "UNSET"`, used for CSP-168 forced setup scenarios). These supersede the old persona names in CI. Password for all: `Capstone26!`.

### Where to add acceptance seed data

The acceptance-specific seed users should be added to `ApplicationDbContextSeeding.SeedDataAsync` gated on an environment check, **or** in a dedicated acceptance-only seeding method called from `TestWebAppHost.StartAsync`. The latter is preferred so production/staging seeding remains unaffected.

`TestWebAppHost.ResetSeedData()` is currently a `NotImplementedException` stub — implementing it to truncate non-badge rows and re-run seed will be required for true scenario isolation once test count grows.

---

## Jira PBI / User Story Guidelines

> The human-readable version of these guidelines lives at `docs/pbi_guidelines.md`.

This section governs how AI assistants (and developers) should create or update Jira issues in the CSP project. All user stories must conform to the **INVEST** principles and follow the established story structure below.

---

Every story must satisfy the six **INVEST** criteria (Independent, Negotiable, Valuable, Estimable, Small, Testable) before being submitted to Jira.

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
