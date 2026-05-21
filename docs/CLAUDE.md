# CLAUDE.md — Project Context for AI Assistants

Structured overview of the **Competitive Wildlife Scavenger App (CWSA)** capstone project. Covers purpose, architecture, conventions, and codebase navigation.

---

## Project Purpose

**CWSA** is a WOU CS senior capstone project (team: "Monmouth Hold'em", class of 2026). "Pokemon GO meets iNaturalist with real competition." Users submit wildlife sightings (GPS + photo), earn points based on species rarity, climb a leaderboard, and earn badges. Admins moderate via a reports system. An Anidex species catalog (backed by API-Ninjas Animals API) enriches sighting data.

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
| Email | Azure Communication Services; `NoOpEmailService` in dev/staging |
| External API | API-Ninjas Animals API — `ExternalApiCaller` with SQL-backed cache |
| Cloud Hosting | Azure App Service + Azure SQL |
| Unit Tests | NUnit 4, Moq, FluentAssertions, coverlet |
| Acceptance/BDD | Reqnroll + NUnit + Selenium ChromeDriver |
| Integration Tests | `Microsoft.AspNetCore.Mvc.Testing` + EF Core In-Memory |
| CI/CD | GitHub Actions |

---

## Architecture

Monolithic ASP.NET Core MVC app split into two source projects:

- **`MH.Capstone.Domain`** — EF Core entities, two `DbContext`s, generic repository, service layer, migrations, constants, tools.
- **`MH.Capstone.WebApp`** — controllers, Razor views, view models, tag helpers. DI wired in `Program.cs`.

**Layering:** `Controller → Service Interface → Service → IRepository<T,TContext> → EF Core → SQL Server`

### DbContexts

Two EF Core DbContexts, same connection string (`DataDb`), separate migrations history tables:

- **`ApplicationDbContext`** (`__EFMigrationsHistory_ApplicationDbContext`) — all app data + Identity. Lazy loading proxies. Seeded via `ApplicationDbContextSeeding`.
- **`CacheDbContext`** — `ApiCallerCacheEntity`, `NinjaAnimalCacheEntity`.

---

## Key Domain Entities

All in `src/MH.Capstone.Domain/DataModels/`:

| Entity | Key Fields |
|---|---|
| `ApplicationUser` | Extends `IdentityUser`. Custom: `Points`, `Bio`, `ProfileImage` (byte[]), `IsDeactivated`, `LastLogin`, `LoginStreak`, `IsStreakActive` |
| `Sighting` | `Latitude`/`Longitude` (DECIMAL 9,6), `Timestamp` (DateTimeOffset), `Description`, `ImageBuffer` (byte[], ≤2 MB), FK to user |
| `Badge` | `Title`, `Description`, `PointValue` (default 10), `BadgeIcon` (byte[]), `HintToEarn` (nvarchar 150, required), `BadgeSteps` (int, default 1 — 1 = single-action badge, >1 = multi-step) |
| `UserBadge` | Join table: user ↔ badge. `BadgeEarned` (DateTimeOffset?, null = not yet earned), `BadgeProgress` (int, default 0, tracks steps completed toward `BadgeSteps`) |
| `Notification` | `Title`, `Message`, `SentAt`, `IsRead`, `IsPostdated` |
| `Report` | `ReportedPageUrl`, `Reason`, `Description`, `IsResolved`. Filtered unique index: no duplicate open reports per user+URL |
| `EmailQueue` | Outbox: `Recipient`, `Subject`, `HtmlBody`, `ScheduledAt`, `IsSent`, `Attempts`, `Processing` |

---

## Services

All interfaces in `src/MH.Capstone.Domain/Services/Abstraction/`:

| Interface | Implementation | Purpose |
|---|---|---|
| `IAuthenticationService` | `AuthenticationService` | Login, register, logout, password reset, email confirmation. New: `GenerateEmailConfirmationTokenAsync(email)`, `ConfirmEmailAsync(email, token)` |
| `IUserService` / `IUserProfileService` | `UserService` | Profile management, deactivation. `UpdateDisplayNameAsync(user, displayName)` validates 2–50 chars, throws `ArgumentOutOfRangeException` if invalid |
| `IProfileImageService` | `ProfileImageService` | Upload/retrieve profile images |
| `ISightingsService` | `SightingsService` | Submit/query sightings. `GetAllSightingsAsync()` eager-loads `User`; `GetUserSightingsAsync(Guid)` filters to one user; `GetSightingByIdAsync(Guid)` (CSP-172) returns `Sighting?` via `IRepository.FindByIdAsync`, lazy-loads `User` on access. `GetSightingsPageAsync(page, pageSize)` (CSP-199) returns `PagedResult<Sighting>` newest-first, pushing `ORDER BY`/`OFFSET-FETCH` to SQL so only one page of rows+images materializes (the gallery perf fix). `page < 1` clamps to 1 |
| `IScoringService` | `ScoringService` | Award points using rarity multiplier |
| `IBadgeService` | `BadgeService` | `AddBadge(user, badgeId, tz)` — awards badge, adds points, sends notification. `UpdateBadge(user, badgeId, tz)` — increments `BadgeProgress` by 1, calls `AddBadge` when `BadgeProgress >= BadgeSteps`. `SyncBadgeProgressAsync(user, badgeId, actualCount, tz)` — idempotent backwards-compat sync: sets `BadgeProgress = actualCount` directly (or calls `AddBadge` if `actualCount >= BadgeSteps`); safe to call on every login. `SortBadgesByTime(list)` — descending chronological sort. |
| `ILeaderboardService` | `LeaderboardService` | `GetLeaderboardPageAsync()` → `IEnumerable<ApplicationUser>`; controller projects to `LeaderboardEntryViewModel` (excludes `Email`) |
| `IReportService` | `ReportService` | Submit and resolve content reports |
| `INotificationService` | `NotificationDispatchService` | Routes notifications by channel. `SendNotificationAsync(notification, type)` — SystemCritical always InAppAndEmail; others consult preference service. Inherits `MarkAllAsReadAsync`, `DeleteAllAsync` from `NotificationServiceBase` |
| `INotificationPreferenceService` | `NotificationPreferenceService` | Per-user, per-type delivery prefs. `GetPreferencesAsync(user)` excludes SystemCritical. `GetDeliveryChannelAsync(user, type)` defaults to `InAppOnly`. `SavePreferencesAsync` silently ignores SystemCritical |
| `IEmailService` | `AzureCommunicationEmailService` / `NoOpEmailService` | Send emails (toggled by `UseRealEmailerService` flag) |
| `ISightingsService` (CSP-142) | `SightingsService` | `GetUserAnidexAsync(Guid)` → `IEnumerable<AnidexEntry>`, one entry per unique `SpeciesName` from the user's sightings. Discovery count is per-user; rarity is derived from the GLOBAL count via `IScoringService`. Sorted rarest-first then alphabetical |
| `IScoringService` (CSP-142) | `ScoringService` | Signature change: `GetGlobalSightingsCountAsync(string speciesName)` (was placeholder `int speciesId`). Case-insensitive match on `Sighting.SpeciesName`; throws on null/whitespace |
| `IApiCaller` | `ExternalApiCaller` | HTTP calls to external APIs with SQL caching |
| `IAnimalFunFactService` (CSP-172) | `AnimalFunFactService` | `GetFunFactAsync(speciesName, ct)` returns `string?`. Wraps `IApiCaller<NinjaApiConfigValues>` against the `"animal"` endpoint. Short-circuits to `null` on null/whitespace/`"Unknown"` species or missing endpoint config; catches all upstream exceptions and returns `null`. Walks fallback chain `slogan → mostDistinctiveFeature → lifestyle → null` from the first DTO's characteristics |
| `IClubService` | `ClubService` | List public/user clubs, pending invites, memberships; create club; send/accept/decline invites; leave club |
| `IAIService` | `GeminiAIService` | `AskAsync(question, ct)` — POSTs to Gemini API, constrained to wildlife/safety topics |
| `IPhotoQualityService` | `PhotoQualityService` | `AnalyzeAsync(bytes, ct)` → `(PhotoQualityTier, Sharpness, Luminance, Width, Height)`. Uses ImageSharp. Sharpness/luminance logic not yet implemented |

**Background service:** `EmailDispatcherService` processes `EmailQueue` outbox.

### Key Endpoints

**Notifications (CSP-138/169):**
- `PUT /notifications/mark-all-read`, `DELETE /notifications/all` — `[ValidateAntiForgeryToken]`, scoped to authenticated user
- `GET/POST /dashboard/notification-preferences` — per-type delivery dropdowns; SystemCritical enforced server-side
- Delivery channels: `Silenced`, `InAppOnly`, `EmailOnly`, `InAppAndEmail`. Default: `InAppOnly`

**Badges (CSP-184):** `GET /dashboard/badges` — `[Authorize]`, renders `BadgesViewModel` with all badges + user's `UserBadges` (full list, not filtered, so in-progress records are available for progress bar display). `DashboardController.Index()` calls `SyncBadgeProgressAsync` for `SightingNovice`, `SightingStudent`, and `AnidexBeginner` on every dashboard load (backwards-compat sync). Dashboard index only passes `earned` badges (`.BadgeEarned.HasValue`) to `SortBadgesByTime` for the recent-badges widget.

**Display name (CSP-168):**
- `GET/POST /account/SetDisplayName` — forced setup for `DisplayName == "UNSET"`
- `POST /dashboard/UpdateDisplayName` — from settings page; redirects to `GET /dashboard/settings`

**Account Settings (CSP-188):** `GET /dashboard/settings` — display name, profile picture, bio, notification preferences link, deactivation link.

**AI Companion (CSP-120):** `POST /AICompanion/Ask` — `[Authorize]`, `[FromForm] string question`, returns `{ reply }`. Returns 503 on API failure.

**Sighting Details (CSP-172):** `GET /Sighting/Details/{id:guid}` — `[Authorize]`. Returns `View("NotFound")` for unknown ids; otherwise builds `SightingDetailsViewModel(sighting, funFact)` and applies the same `UserTimeZone` cookie convention as Gallery so the displayed timestamp matches the card the user clicked.

**RequireDisplayNameFilter:** Global `IAsyncActionFilter` redirecting `DisplayName == "UNSET"` users to `Account/SetDisplayName`. Exempted: `SetDisplayName`, `Login`, `Logout`, `Register`, `RegisterConfirmation`, `VerifyEmail`, `ResendVerification`, `ForgotPassword`, `ResetPassword`, `Reactivate`, `Deactivate`, test email endpoints.

---

## Scoring Logic

`ScoringService` awards points per sighting:
- **Base:** 10 pts
- **Multiplier** by global sightings of that species: Mythic (≤5) = 5×, Rare (≤50) = 2×, Common (>50) = 1×

---

## Controllers

| Controller | Responsibility |
|---|---|
| `AccountController` | Register, login, logout, profile |
| `AdminController` | Admin-only views |
| `DashboardController` | User dashboard (points, badges, recent activity) |
| `HomeController` | Landing pages |
| `LeaderboardController` | Global rankings |
| `MapController` | GPS sighting map (Leaflet.js) |
| `ReportControllers` | Submit/view content reports |
| `SightingController` | Submit/view wildlife sightings. `Details(Guid id)` action (CSP-172) — depends on `ISightingsService` and `IAnimalFunFactService` |
| `SpeciesController` | Animal lookup catalog (API-Ninjas Animals API, cached) |
| `AnidexController` (CSP-142) | `GET /anidex` — personal Anidex page; gallery of unique species from the authenticated user's sightings. `[Authorize]` |
| `ClubsController` | Club listing, creation, chatroom stubs |
| `AICompanionController` | Gemini AI chat endpoint |

---

## Feature Flags

Singleton from `appsettings.json`:

| Flag | Effect |
|---|---|
| `UseRealEmailerService` | `true` → `AzureCommunicationEmailService`; else `NoOpEmailService` |
| `EnableEmailTestEndpoint` | Exposes `GET /Account/GeneratePasswordResetLink` and `GET /Account/GenerateEmailConfirmationLink`; always forced `true` by `TestWebAppHost` |
| `EnableGeminiAIService` | `true` → registers `GeminiAIService`; else AI Companion fails at controller |
| `ExposeDetailedApiCacheOnUi` | Defined; no usage wired yet |

---

## Testing Strategy

### Unit Tests
- **`Domain.Tests.Unit`** — service isolation with Moq. Covers: `AuthenticationService`, `BadgeService`, `LeaderboardService`, `ScoringService`, `SightingsService`, `UserService`, `ReportService`, `NotificationService`, `ExternalApiCaller`, `GeminiAIService`.
- **`WebApp.Tests.Unit`** — controllers and view models in isolation.

### Integration Tests
**`Tests.Integration`** — `Microsoft.AspNetCore.Mvc.Testing` + EF Core In-Memory. Covers leaderboard, reports, sightings gallery.

### Acceptance Tests (BDD)
**`Tests.Acceptance`** — Reqnroll feature files in `Features/`, step definitions in `StepDefinitions/`, Selenium page objects in `PageObjects/`, drivers in `Drivers/`. `TestWebAppHost.cs` auto-starts the app.

#### Acceptance Test Infrastructure

- **Environment:** `ASPNETCORE_ENVIRONMENT = "Acceptance"`. Kestrel on `https://localhost:5001`.
- **Database:** Real SQL Server LocalDB (`WAID_AppDataDb`) — not InMemory. Migrations + seeding run on startup.
- **Config load order:** `appsettings.json` → `appsettings.Acceptance.json` → `appsettings.Acceptance.Local.json` (gitignored) → env vars.
- **Browser:** One shared `ChromeDriver` for the entire run (`BeforeTestRun`/`AfterTestRun`).
- **Scenario isolation:** `TestWebAppHost.ResetSeedDataAsync()` is fully implemented — it delegates to `AcceptanceTestSeeder.SeedAsync`, which `ExecuteDeleteAsync`'s every application table in FK-safe order and re-inserts the canonical fixtures. Features that can't tolerate persistent DB state (e.g. accumulated sightings/badges from other features touching the same persona) opt in with a `[BeforeScenario(<tag>)] public static async Task` hook that calls `await TestWebAppHost.ResetSeedDataAsync()`. Existing adopters: CSP-133, CSP-138, CSP-184.
- **DI in steps:** Per-scenario DI via `[ScenarioDependencies]` in `TestDependencySetup`. Every new Driver must be registered as `services.AddTransient<TDriver>()` — Reqnroll does not auto-discover.
- **Password reset pattern:** `PasswordResetDriver.GetPasswordResetLink(email)` → navigate to URL (mimics email click).
- **Email confirmation pattern:** `EmailVerificationDriver.GetEmailConfirmationLink(email)` → navigate to URL. Fresh unverified users use `csp134_{guid}@test.com`.
- **Registration UX (CSP-134):** Registration sends verification email and redirects to `/Account/RegisterConfirmation`. Users must verify before login. Seeded personas have `EmailConfirmed = true`.

#### Seed Personas

`CSP53StepDefinitions` hard-codes **`alpha@test.com` / `Capstone26!`** — must exist with `User` role.

Active seeder (`AcceptanceTestSeeder.cs`) — password `Capstone26!`, all `EmailConfirmed = true`, fixed GUIDs:

| Persona | Email | GUID prefix | Notes |
|---|---|---|---|
| Alex | `alex@test.com` | `aaaaaaaa-...` | Primary logged-in user for newer scenarios |
| Patricia | `patricia@test.com` | `bbbbbbbb-...` | |
| Lily | `lily@test.com` | `cccccccc-...` | |
| Owen | `owen@test.com` | — | `DisplayName = "UNSET"`, used for CSP-168 forced setup |
| James | (none) | — | Unauthenticated visitor |

Legacy personas still coexist: `alpha@test.com`, `alice@test.com`, `bob@test.com`, `newuser@test.com`, `admin@test.com`.

#### Page Element IDs

| Page | Element ID | Purpose |
|---|---|---|
| Any | `userDropdownNavDisplay` | Detect logged-in user (nav bar) |
| Any | `navDisplayNameText` | `<span>` with display name only (excludes notification badge count) |
| Any | `logoutBtn` | Logout button |
| `/Account/Login` | `emailField`, `passwordField`, `RememberMe`, `submitBtn` | Login form |
| `/Account/Login` | `passwordResetSuccessMessage` | Success banner after password reset |
| `/Account/Login` | `emailNotVerifiedMessage`, `resendVerificationBtn` | Unverified user warning |
| `/Account/ForgotPassword` | `forgotPasswordEmail`, `sendResetEmailBtn`, `resetEmailSentMessage` | Forgot password flow |
| `/Account/ResetPassword` | `newPasswordField`, `confirmPasswordField`, `resetPasswordBtn`, `resetPasswordError` | Reset password form |
| `/Account/ResetPasswordInvalid` | `invalidResetLinkMessage`, `requestNewResetLinkBtn` | Invalid link page |
| `/Account/RegisterConfirmation` | `registrationConfirmationMessage`, `resendFromConfirmationLink` | Post-registration confirmation |
| `/Account/VerifyEmail` | `emailVerifiedSuccessMessage`, `loginAfterVerificationBtn`, `emailVerificationErrorMessage`, `requestNewVerificationBtn` | Email verification |
| `/Account/ResendVerification` | `resendVerificationEmail`, `resendVerificationSubmitBtn`, `resendVerificationSentMessage` | Resend verification |
| `/Account/Register` | `displayNameField` | Display name input on registration |
| `/Account/SetDisplayName` | `setDisplayNameField`, `setDisplayNameBtn` | Forced setup page |
| `/Sighting/Create` | `Latitude`, `Longitude`, `Timestamp`, `Description`, `UploadedImage`, `SubmitBtn` | Sighting submission form |
| `/Sighting/Create` | `SpeciesName` | CSP-142: required species text input; auto-filled from CSP-144 AI suggestion when empty |
| `/Sighting/Gallery` | `filterAll`, `filterMine`, `emptyStateMine`, `sightingsGrid`, `currentUserId` | Gallery filters/state |
| `/Sighting/Gallery` | `galleryPagination`, `pagePrev`, `pageNext` | CSP-199: pagination nav, only rendered when `TotalPages > 1`. Gallery is paginated server-side at **20/page** (`GalleryPageSize` const in `SightingController`); `SightingController.Gallery(int page = 1)` calls `GetSightingsPageAsync`. `SightingGalleryViewModel` carries `CurrentPage`/`PageSize`/`TotalCount`/`TotalPages`/`HasPreviousPage`/`HasNextPage`. **Known limitation:** the `filterAll`/`filterMine` JS toggle filters the current page only — server-side filtering across pages is a deferred follow-up |
| `/Sighting/Gallery` | `.sighting-card-wrapper[data-user-id]` | Per-card wrapper with user attribution |
| `/Sighting/Gallery` | `.sighting-attribution` | Submitter's `DisplayName` span |
| `/Sighting/Gallery` | `a.sighting-card-link[data-sighting-id]` | CSP-172: anchor wrapping each card; navigates to `/Sighting/Details/{id}` |
| `/Sighting/Details/{id}` | `sightingDetailsContainer` | CSP-172: root container — its presence signals the page rendered |
| `/Sighting/Details/{id}` | `sightingDetailsImage` | Full-resolution sighting image |
| `/Sighting/Details/{id}` | `sightingDetailsUploaderIcon`, `sightingDetailsUploaderName` | Uploader avatar + display name |
| `/Sighting/Details/{id}` | `sightingDetailsSpecies`, `sightingDetailsTimestamp`, `sightingDetailsLocation`, `sightingDetailsDescription` | Sighting metadata |
| `/Sighting/Details/{id}` | `sightingDetailsFunFact` | Fun fact text. Carries `data-fun-fact-status="ok"` normally, `"fallback"` when `IsFunFactFallback == true` |
| `/Sighting/Details/{id}` | `backToGalleryLink` | Link back to Gallery |
| `/Sighting` (NotFound view) | `sightingNotFoundMessage`, `notFoundBackToGalleryLink` | CSP-172: rendered by `Details` action when the id does not resolve |
| Sightings dropdown | `anidexNavLink` | CSP-142: "My Anidex" entry (authenticated only) |
| `/anidex` | `anidexEmptyState`, `anidexGrid`, `anidexSpeciesCount` | CSP-142: page state containers |
| `/anidex` | `.anidex-entry`, `.anidex-species-name`, `.anidex-rarity-badge`, `.anidex-discovery-count` | CSP-142: per-species card selectors (entry carries `data-species-name`) |
| `/dashboard` | `accountSettingsLink` | Link to Account Settings page |
| `/dashboard/badges` | `currentUserId` | Hidden field with logged-in user's GUID (page-load guard) |
| `/dashboard/badges` | `.badge-card` | Per-badge card container |
| `/dashboard/badges` | `.badge-step-count` | `<small>` showing `currentProgress / BadgeSteps` for multi-step badges |
| `/dashboard/badges` | `.badge-card.border-success` | Card border applied when badge is earned |
| `/dashboard/badges` | `span.badge.bg-success` | "Earned" label shown on earned badge cards |
| `/dashboard/settings` | `displayNameInput`, `updateDisplayNameBtn`, `displayNameSuccessMessage` | Display name section |
| `/dashboard/settings` | `notificationPreferencesLink` | Link to notification preferences |
| `/dashboard/notification-preferences` | `notificationPreferencesForm`, `saveNotificationPreferencesBtn`, `notificationPreferenceSuccess` | Preferences form |
| `/dashboard/notification-preferences` | `pref_{NotificationType}` | `<select>` per type (e.g. `pref_BadgeAwarded`) |
| `/notifications` | `markAllReadForm`, `markAllReadBtn`, `deleteAllForm`, `deleteAllBtn`, `notificationsEmptyState` | Notification list controls |
| `_Layout.cshtml` | `aiCompanionModal`, `aiCompanionForm`, `aiCompanionQuestion`, `aiCompanionSubmitBtn`, `aiCompanionMessages` | AI Companion modal |
| `/Species/Search` | `nameInput`, `clearBtn`, `searchForm`, `searchStatus`, `resultCard`, `resultCounter`, `prevBtn`, `nextBtn` | Wildlife search |

Access-denied detection: `driver.Url` contains `/account/login` (case-insensitive).

---

## PBI Implementation Workflow

"Done" = feature code + tests. Follow **Red/Green/Refactor**:
1. **Red** — failing test capturing the requirement
2. **Green** — minimal implementation to pass
3. **Refactor** — clean up without breaking (skip if nothing to clean)
4. **Commit** — one commit per TDD cycle; one scenario per BDD commit

**Commit conventions:**
```
[CSP-XXX] <what was implemented> (TDD)
[CSP-XXX] BDD: <scenario name from .feature file>
```

### Test Requirements per PBI
- **Unit tests** — all new/modified service methods and controller actions (NUnit + Moq + FluentAssertions)
- **BDD/Acceptance tests** — at least one Reqnroll `.feature` scenario per acceptance criterion (Selenium end-to-end)

### Pull Request Conventions

- **Base branch:** always `dev` (`--base dev`) — never `main`
- **Reviewer:** `jmcshane22`
- **Assignees:** `jmcshane22` and `beastmode24jd`
- **Draft:** always `--draft`
- **Labels:** check `gh label list --repo jmcshane22/MonmouthHoldemCapstone`

#### `gh pr edit` is broken — use REST API instead

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

# Convert back to draft
gh pr ready {n} --repo jmcshane22/MonmouthHoldemCapstone --undo
```

### Post-Implementation: Update This File

After every PBI: add new element IDs, update service/interface descriptions, note new ViewModel properties, document new Drivers/PageObjects, record non-obvious patterns.

---

## CI/CD

**`pr_deploy_and_merge.yml`** — triggers on Approved review of non-draft PR targeting `dev` or `main`. Runs full test suite → publish → deploy → auto-merge → GitHub Release. `dev` → `azure_staging` (prerelease); `main` → `azure_prod`.

**`deploy.yml`** — manual `workflow_dispatch`. Same pipeline, no PR required.

**`test_suite_complete_run.yml`** — full suite (build + unit + EF + integration + acceptance). Called by PR workflow; triggerable manually.

**`manual_pr_test_run.yml`** — triggers full suite against a specific PR number (before approval).

Reusable sub-workflows: `build.yml`, `unit_tests.yml`, `ef_core_tests.yml`, `system_tests.yml`, `test_suite_limited_run.yml`.

Build versioning: `YYYY.M.<run_number>.<run_attempt>`

### Running Tests via CI (for AI Agents)

Tests do **not** run on push/PR open. Use manual dispatch:

```bash
gh workflow run manual_pr_test_run.yml \
  --repo jmcshane22/MonmouthHoldemCapstone \
  --ref dev \
  --field pr_number=<PR_NUMBER>
```

Appears under **"Run Complete Test Suite (All Tests)"**. Takes 10+ minutes. Poll:

```bash
gh run list --repo jmcshane22/MonmouthHoldemCapstone \
  --workflow test_suite_complete_run.yml --limit 5
```

---

## Configuration Notes

- Connection string: `DataDb` (both DbContexts)
- Azure Communication Services: `ConnectionStrings:AzureCommunicationServices`
- Email sender: `Email:SenderAddress`
- Password policy: min 8 chars, requires digit, upper, lower, non-alphanumeric
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
| Gemini options | `src/MH.Capstone.Domain/ApiContracts/Gemini/GeminiOptions.cs` |
| Acceptance test features | `src/MH.Capstone.Tests.Acceptance/Features/` |
| Acceptance test seeder | `src/MH.Capstone.Tests.Acceptance/Seeding/AcceptanceTestSeeder.cs` |
| Acceptance testing guide | `docs/acceptance_testing.md` |
| Architectural guidelines | `docs/architectural_guidelines.md` |

---

## Database Schema

Non-obvious constraints:

- `ApplicationUser.DisplayName`: `nvarchar(50)`, required, defaults to `"UNSET"`; 2–50 chars. `IsStreakActive` is computed (not-mapped): true when `(UtcNow − LastLogin) ≤ 30 days`.
- `Sighting.ImageBuffer`: `varbinary(max)`, required (1 byte – 2 MB). `Timestamp` must be past (`[PastDateTime]`).
- `Sighting.SpeciesName` (CSP-142): `nvarchar(100)`, required; default `"Unknown"` for migrated rows. Captured at upload (manual entry or AI suggestion). Anidex grouping + species-keyed rarity scoring depend on this column.
- `Report`: unique filtered index on `(ReportingUserId, ReportedPageUrl)` where `IsResolved = 0`.
- `EmailQueue`: composite index on `(IsSent, ScheduledAt)`; `Processing` = dispatcher lock.

**Seeded roles:** `User`, `Admin`. **Seeded badges (idempotent upsert via `ApplicationDbContextSeeding.cs`):**

| Constant | GUID | Title | Points | BadgeSteps |
|---|---|---|---|---|
| `BadgeId.ProfileBadgeGUID` | `A1B2C3D4-E5F6-4789-8A9B-0C1D2E3F4A5B` | Custom Profile Badge | 10 | 1 |
| `BadgeId.CustomBioBadgeGUID` | `91E7773E-F6D7-457E-911E-8246891D65A2` | Custom Bio Badge | 10 | 1 |
| `BadgeId.FirstSightingBadgeGUID` | `B2C3D4E5-F6A7-4890-9B0C-1D2E3F4B5A6F` | First Sighting Badge | 25 | 1 |
| `BadgeId.SightingNoviceBadgeGUID` | `27857EC5-189E-46E8-BE28-871123607F20` | Sighting Novice | 35 | 5 |
| `BadgeId.SightingStudentBadgeGUID` | `8436745D-C25B-44BF-A0E1-0C87E6122724` | Sighting Student | 50 | 25 |
| `BadgeId.AnidexBeginnerBadgeGUID` | `C3D4E5F6-A7B8-4901-AC1D-2E3F4B5A6F7E` | Anidex Beginner | 35 | 5 |

FK seeding order: `AspNetRoles` → `AspNetUsers` → `Badge` → `Sighting`/`PersonalBadges`/`Notification`/`Report` → `EmailQueue`.

### Extended Sighting Columns

| Column | Type | Migration | Notes |
|---|---|---|---|
| `PointValue` | int | CSP-109 `20260412175159_AddSightingScoringMetadata` | Points awarded; default 10 |
| `LoginStreak` | bit | CSP-109 | Streak active at submission |
| `Rarity` | nvarchar | CSP-109 | Frozen tier label; default `"Common"` |
| `RarityMultiplier` | float | CSP-109 | Frozen multiplier; default `1.0` |
| `QualityTier` | int | CSP-122 `20260421231908_CSP122_AddPhotoQualityFields` | `PhotoQualityTier` enum; default 0 (Unknown) |
| `SharpnessScore` | float? | CSP-122 | Nullable |
| `LuminanceAverage` | float? | CSP-122 | Nullable |
| `ResolutionWidth/Height` | int? | CSP-122 | Nullable |
| `FlaggedForReview` | bit | CSP-122 | Default false |

`20260414223118_FixSightingUserIdType` corrected `Sighting.UserId` FK column type. **Note:** the generated `AlterColumn` was replaced with idempotent raw SQL (`IF EXISTS` guard on index drop) because the index did not exist on fresh databases — if this migration is re-generated, the raw SQL must be restored.

`20260505201153_AddHintAndProgressionFieldsToBadges` added `HintToEarn` (nvarchar 150) and `Badge Steps` (int, default 1) to `Badge`; added `Badge Progress` (int, default 0) to `PersonalBadges`.

### Photo Quality (CSP-122)

`PhotoQualityTier` enum: `Unknown=0, Low=1, Medium=2, High=3`. Sharpness/luminance not yet implemented — `PhotoQualityService` returns `Unknown/0.0/0.0` plus real dimensions. Acceptance steps in `CSP122StepDefinitions.cs` (`@photo-quality`) throw `NotImplementedException`. Validation thresholds (when implemented): luminance `0.20–0.85`, high resolution ≥ 2048 px on long side.

### Sighting Details (CSP-172)

Drill-down view from the Sightings Gallery. Clicking a card navigates to `/Sighting/Details/{id}`, which renders the full image, uploader attribution, sighting metadata, and a fun fact about the identified species pulled via `IAnimalFunFactService`.

**`SightingDetailsViewModel`** (`WebApp/Models/`): constructor takes `(Sighting, string? funFact)`. Encapsulates the fun-fact fallback decision — when `funFact` is null/whitespace it sets `IsFunFactFallback = true` and substitutes the canonical `FunFactFallbackMessage` ("Fun facts about this animal aren't available right now."). The view reads `IsFunFactFallback` to set `data-fun-fact-status="fallback"` on the fun-fact element. Default avatar path is `/imgs/profileDefault.jpg` (matches `Extensions.GetProfileImageUrl()` convention).

**CSP-184 Badges page:**
- `Features/CSP-184.feature` — 4 scenarios: nav bar link visible, no-progress badge greyed with hint, partial-progress shows progress bar + step count, action advances badge and page updates
- `StepDefinitions/CSP184StepDefinitions.cs` — Scenario 3 navigates to `/dashboard` first (triggers `SyncBadgeProgressAsync`), then checks `.badge-step-count`. Scenario 4 asserts Sighting Novice card specifically via XPath `ancestor::div[contains(@class,'badge-card')]` from the title text, checks `border-success` class and `span.badge.bg-success` visibility. `[BeforeScenario("badge")]` calls `TestWebAppHost.ResetSeedDataAsync()` so Alex starts each scenario at exactly his 4 seeded sightings — without this, sightings other features submit as Alex (CSP-53/122/125/141/144/193) push him past the 5-sighting Sighting Novice threshold and Scenario 3's progress bar disappears.
- No new Driver or PageObject — scenarios use `_driver`/`_wait` directly.

**Acceptance infrastructure:**
- `Features/CSP-172.feature` — 4 scenarios: Patricia happy path, Alex fun-fact fallback (uses Mystery Critter Z), James anonymous redirect, Lily not-found
- `PageObjects/SightingDetailsPageObject` — selectors for the details + not-found pages
- `Drivers/SightingDetailsDriver` — `NavigateToDetails(Guid)`, `ClickFirstSightingCardLink()`, `IsOnDetailsPage()`, `IsOnNotFoundPage()`. **Registered in `TestDependencySetup` as `AddTransient<SightingDetailsDriver>()`** — Reqnroll won't auto-discover.
- `StepDefinitions/CSP172StepDefinitions` — adds two new step bindings reusable by future features: `Given user Lily is logged in` and `Given visitor James is signed out`.

**DI:** `IAnimalFunFactService` is registered scoped in `Program.cs` next to `IPhotoQualityService` (no feature flag).

---

## Test Seed Data Guidance

- `Sighting.ImageBuffer` required and non-empty — use `new byte[] { 0x01 }`
- `Sighting.Timestamp` must be past — use `DateTimeOffset.UtcNow.AddDays(-N)`
- `Report` unique filtered index — stagger `IsResolved` or use different URLs per user
- `UserBadge` requires `Badge` row first
- `ApplicationUser.Id` is GUID as string — use fixed GUIDs in seed data
- Passwords: min 8 chars, digit, upper, lower, non-alphanumeric (e.g. `Capstone26!`)
- Users need `NormalizedEmail`, `NormalizedUserName` (`.ToUpper()`), hashed password via `PasswordHasher<ApplicationUser>`
- Role assignments via `AspNetUserRoles`

**`SightingUploadViewModel`:** carries `DeviceTimezone` (default `"America/Los_Angeles"`). `ToDataModel()` converts local timestamp to UTC. Tests/seeds using this view model must set `DeviceTimezone`.

**`BadgesViewModel`** (`WebApp/Models/`): `AllBadges` (all `Badge` rows, alpha-sorted), `UserBadges` (full `user.UserBadges` list — includes in-progress records so the progress bar has data), `CurrentUserId` (Guid, used as page-load guard in tests).

**CSP-184 acceptance seeder:** `AcceptanceTestSeeder.cs` seeds `SightingNoviceBadge` (`BadgeSteps = 5`). `SightingStudentBadge` and `AnidexBeginnerBadge` are **not yet** in the acceptance seeder — add them before writing acceptance tests for those badges.

**`NotDefaultCoordinatesAttribute`:** class-level `ValidationAttribute` failing when both `Latitude` and `Longitude` are exactly `0.0`. Applied to `SightingUploadViewModel`.

> **Seeded species names (CSP-142):** Alex's 3 sightings are `Great Blue Heron` ×2 + `Bald Eagle` ×1 (so his Anidex contains 2 entries — discovery count for "Great Blue Heron" is 2). Lily's 5 sightings are distinct species: `Wolverine`, `Peregrine Falcon`, `River Otter`, `Roosevelt Elk`, `Coyote`. CSP-142 BDD scenarios assert against these names — don't rename without updating `Features/CSP-142.feature`.

> **CSP-172 fallback-species sighting:** Alex has a 4th seeded sighting (id `a1000000-0000-0000-0000-000000000004`) with `SpeciesName = "Mystery Critter Z"` — a deliberately unmatchable string the Animals API will never resolve. The CSP-172 Alex scenario navigates directly to this sighting's details URL to exercise the fun-fact fallback path. Don't rename this species or change the id without updating `CSP172StepDefinitions.AlexUnresolvedSpeciesSightingId`.

---

## Jira PBI Guidelines

Full template and checklist: `docs/pbi_guidelines.md`. Key points for AI agents:

- Story format: `As a <role>, when <context>, I want <goal> so that <benefit>.`
- Team: "MH Development Team"; SPE: 1, 2, or 4 (powers of 2)
- Acceptance criteria in Gherkin `Given/When/Then`; every requirement needs a scenario
- Append to every created/modified PBI description:

```
---
AI Agent <Agent Name> assisted in the creation and/or modification of this PBI.
```
