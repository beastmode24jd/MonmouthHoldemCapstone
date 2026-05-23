# CLAUDE.md — Project Context for AI Assistants

Structured overview of the **Competitive Wildlife Scavenger App (CWSA)** capstone project.

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
```

---

## Technology Stack

| Category | Technology |
|---|---|
| Runtime | .NET 9 (SDK 9.0.309) |
| Web Framework | ASP.NET Core MVC |
| ORM | EF Core 9 + SQL Server (lazy loading proxies on `ApplicationDbContext`) |
| Frontend | Bootstrap 5.3, Leaflet.js (OpenStreetMap) |
| Email | Azure Communication Services; `NoOpEmailService` in dev/staging |
| External API | API-Ninjas Animals API — `ExternalApiCaller` with SQL-backed cache |
| Cloud | Azure App Service + Azure SQL |
| BDD | Reqnroll + Selenium ChromeDriver |
| Integration Tests | `Microsoft.AspNetCore.Mvc.Testing` + EF Core In-Memory |

---

## Architecture

**Layering:** `Controller → Service Interface → Service → IRepository<T,TContext> → EF Core → SQL Server`

Two EF Core DbContexts, **same connection string** (`DataDb`), separate migrations history tables:
- **`ApplicationDbContext`** (`__EFMigrationsHistory_ApplicationDbContext`) — all app data + Identity. Seeded via `ApplicationDbContextSeeding`.
- **`CacheDbContext`** — `ApiCallerCacheEntity`, `NinjaAnimalCacheEntity`.

---

## Key Domain Entities

All in `src/MH.Capstone.Domain/DataModels/`. Non-obvious fields only:

| Entity | Notable Fields |
|---|---|
| `ApplicationUser` | `IsStreakActive` is not-mapped (computed: `UtcNow − LastLogin ≤ 30 days`). `ProfileImage` is `byte[]`. |
| `Sighting` | `Latitude`/`Longitude` DECIMAL 9,6. `Timestamp` is `DateTimeOffset`. `ImageBuffer` byte[] ≤ 2 MB. |
| `Badge` | `BadgeSteps`: 1 = single-action, >1 = multi-step. `HintToEarn` nvarchar 150, required. |
| `UserBadge` | `BadgeEarned` null = not yet earned. `BadgeProgress` tracks steps toward `BadgeSteps`. |
| `Report` | `ReportingUserId` is `[NotMapped]` — wrapper around mapped string column `ReportingUserIdentityId`. Unique filtered index: no duplicate open reports per user+URL. `SubmittedAt` is `DateTimeOffset` (supports timezone conversion for display). |
| `EmailQueue` | `Processing` = dispatcher lock. Composite index on `(IsSent, ScheduledAt)`. |

---

## Services

All interfaces in `src/MH.Capstone.Domain/Services/Abstraction/`:

| Interface | Purpose |
|---|---|
| `IAuthenticationService` | Login, register, logout, password reset, email confirmation |
| `IUserService` / `IUserProfileService` | Profile management, deactivation. `UpdateDisplayNameAsync` validates 2–50 chars, throws `ArgumentOutOfRangeException` |
| `IProfileImageService` | Upload/retrieve profile images |
| `ISightingsService` | Submit/query sightings. `GetAllSightingsAsync()` eager-loads User. `GetUserAnidexAsync(Guid)` → one entry per unique `SpeciesName`; rarity from global count, sorted rarest-first. `GetSightingsPageAsync(page, pageSize)` (CSP-199) → `PagedResult<Sighting>` newest-first, pushes `ORDER BY`/`OFFSET-FETCH` to SQL so only one page of rows+images loads (gallery perf fix); `page < 1` clamps to 1. |
| `IScoringService` | Award points by rarity. `GetGlobalSightingsCountAsync(string)` — case-insensitive match on `SpeciesName`. |
| `IBadgeService` | `SyncBadgeProgressAsync(user, badgeId, actualCount, tz)` — idempotent, safe to call on every login. `UpdateBadge` increments progress, calls `AddBadge` when threshold met. `SortBadgesByTime` — descending chronological sort. |
| `ILeaderboardService` | `GetLeaderboardPageAsync()` → `IEnumerable<ApplicationUser>` |
| `IReportService` | `SubmitReportAsync` returns `false` on duplicate (unique constraint), throws `ArgumentException` on validation failure. `SortReports` rewrites each `SubmittedAt` to caller's `TimeZoneInfo` for display; `UserSearch` param is DisplayName (not id). `SetReportResolution(id, isResolved)` assigns `IsResolved` directly — does not toggle. |
| `INotificationService` | `NotificationDispatchService` — SystemCritical always InAppAndEmail; others consult preference service |
| `INotificationPreferenceService` | Per-user delivery prefs. `GetDeliveryChannelAsync` defaults to `InAppOnly`. `SavePreferencesAsync` silently ignores SystemCritical. |
| `IEmailService` | `AzureCommunicationEmailService` / `NoOpEmailService` (toggled by feature flag) |
| `IAnimalFunFactService` | `GetFunFactAsync(speciesName, ct)` → `string?`. Short-circuits on null/whitespace/`"Unknown"`. Fallback chain: `slogan → mostDistinctiveFeature → lifestyle → null`. Catches all upstream exceptions, returns `null`. |
| `IClubService` | List/create clubs, invites, memberships |
| `IAIService` | `GeminiAIService.AskAsync` — constrained to wildlife/safety topics |
| `IPhotoQualityService` | `AnalyzeAsync` → `(PhotoQualityTier, Sharpness, Luminance, Width, Height)`. Sharpness/luminance **not yet implemented** — returns `Unknown/0.0/0.0` + real dimensions. |

**Background service:** `EmailDispatcherService` processes `EmailQueue` outbox.

---

## Key Endpoints

| Route | Notes |
|---|---|
| `GET /dashboard` | Calls `SyncBadgeProgressAsync` for SightingNovice, SightingStudent, AnidexBeginner on every load |
| `GET /dashboard/badges` | `BadgesViewModel` passes full `UserBadges` list (not filtered to earned) so progress bar has data |
| `GET/POST /account/SetDisplayName` | Forced for `DisplayName == "UNSET"` |
| `GET /Sighting/Details/{id:guid}` | `[Authorize]`. Reads `UserTimeZone` cookie (same convention as Gallery). Returns `View("NotFound")` for unknown ids. |
| `POST /Admin/UpdateResolution/{id}` | AJAX; `status` from querystring, antiforgery token from `input[name="__RequestVerificationToken"]`. |

**`RequireDisplayNameFilter`:** Global filter redirecting `DisplayName == "UNSET"` users. Exempted: `SetDisplayName`, `Login`, `Logout`, `Register`, `RegisterConfirmation`, `VerifyEmail`, `ResendVerification`, `ForgotPassword`, `ResetPassword`, `Reactivate`, `Deactivate`, test email endpoints.

---

## Scoring Logic

Base 10 pts. Multiplier by global sightings of that species: Mythic (≤5) = 5×, Rare (≤50) = 2×, Common (>50) = 1×.

---

## Controllers

| Controller | Responsibility |
|---|---|
| `AccountController` | Register, login, logout, profile |
| `AdminController` | `[Authorize(Roles="Admin")]`. Report queue reads `UserTimeZone` cookie (default `America/Los_Angeles`; Windows fallback `Pacific Standard Time`). Role promotion/demotion re-verifies admin password and removes existing roles before assigning. `SearchUsers` AJAX endpoint returns `{email, displayName}[]` filtered by `AccountLocked`. |
| `DashboardController` | User dashboard (points, badges, recent activity) |
| `HomeController` | Landing pages |
| `LeaderboardController` | Global rankings |
| `MapController` | GPS sighting map (Leaflet.js) |
| `ReportController` | Submit/view content reports |
| `SightingController` | Submit/view sightings. `Details(Guid)` depends on `ISightingsService` + `IAnimalFunFactService`. |
| `AnidexController` | `GET /anidex` — `[Authorize]`, personal species discovery gallery |
| `SpeciesController` | Animal lookup via API-Ninjas, cached |
| `ClubsController` | Club listing, creation, chatroom stubs |
| `AICompanionController` | `POST /AICompanion/Ask` — `[Authorize]`, returns `{ reply }`, 503 on API failure |

---

## Feature Flags

Singleton from `appsettings.json`:

| Flag | Effect |
|---|---|
| `UseRealEmailerService` | `true` → `AzureCommunicationEmailService`; else `NoOpEmailService` |
| `EnableEmailTestEndpoint` | Exposes password reset / email confirmation test links; forced `true` by `TestWebAppHost` |
| `EnableGeminiAIService` | `true` → registers `GeminiAIService`; else AI Companion fails at controller |

---

## Testing Strategy

### Unit Tests
- **`Domain.Tests.Unit`** — service isolation with Moq + FluentAssertions
- **`WebApp.Tests.Unit`** — controllers and view models in isolation

### Integration Tests
`Microsoft.AspNetCore.Mvc.Testing` + EF Core In-Memory. Covers leaderboard, reports, sightings gallery.

### Acceptance Tests (BDD)
Reqnroll + Selenium. `ASPNETCORE_ENVIRONMENT = "Acceptance"`. Kestrel on `https://localhost:5001`.

**Database:** Real SQL Server LocalDB (`WAID_AppDataDb`) — migrations + seeding run on startup. Not InMemory.

**Config load order:** `appsettings.json` → `appsettings.Acceptance.json` → `appsettings.Acceptance.Local.json` (gitignored) → env vars.

**Scenario isolation:** `TestWebAppHost.ResetSeedDataAsync()` delegates to `AcceptanceTestSeeder.SeedAsync` — `ExecuteDeleteAsync`'s every table in FK-safe order, then re-seeds. Opt in with `[BeforeScenario(<tag>)]`. Existing adopters: CSP-133, CSP-138, CSP-184.

**DI in steps:** Per-scenario DI via `[ScenarioDependencies]` in `TestDependencySetup`. **Every new Driver must be registered as `services.AddTransient<TDriver>()`** — Reqnroll does not auto-discover.

**Reusable step bindings (CSP-172):** `Given user Lily is logged in`, `Given visitor James is signed out`.

#### Seed Personas

`CSP53StepDefinitions` hard-codes **`alpha@test.com` / `Capstone26!`** — must exist with `User` role.

Active seeder — password `Capstone26!`, all `EmailConfirmed = true`:

| Persona | Email | GUID prefix | Notes |
|---|---|---|---|
| Alex | `alex@test.com` | `aaaaaaaa-...` | Primary logged-in user for newer scenarios |
| Patricia | `patricia@test.com` | `bbbbbbbb-...` | |
| Lily | `lily@test.com` | `cccccccc-...` | |
| Owen | `owen@test.com` | — | `DisplayName = "UNSET"`, used for CSP-168 |
| James | (none) | — | Unauthenticated visitor |

Legacy personas still coexist: `alpha@test.com`, `alice@test.com`, `bob@test.com`, `newuser@test.com`, `admin@test.com`.

#### Page Element IDs

| Page | Element ID | Purpose |
|---|---|---|
| Any | `userDropdownNavDisplay` | Detect logged-in user (nav bar) |
| Any | `navDisplayNameText` | Display name span (excludes notification badge count) |
| Any | `logoutBtn` | Logout button |
| `/Account/Login` | `emailField`, `passwordField`, `RememberMe`, `submitBtn` | Login form |
| `/Account/Login` | `emailNotVerifiedMessage`, `resendVerificationBtn` | Unverified user warning |
| `/Account/SetDisplayName` | `setDisplayNameField`, `setDisplayNameBtn` | Forced setup page |
| `/Sighting/Create` | `Latitude`, `Longitude`, `Timestamp`, `Description`, `UploadedImage`, `SubmitBtn`, `SpeciesName` | Sighting submission form |
| `/Sighting/Gallery` | `filterAll`, `filterMine`, `emptyStateMine`, `sightingsGrid`, `currentUserId` | Gallery filters/state |
| `/Sighting/Gallery` | `.sighting-card-wrapper[data-user-id]`, `.sighting-attribution` | Per-card user attribution |
| `/Sighting/Gallery` | `a.sighting-card-link[data-sighting-id]` | Card link → `/Sighting/Details/{id}` |
| `/Sighting/Gallery` | `galleryPagination`, `pagePrev`, `pageNext` | CSP-199: pagination nav, rendered only when `TotalPages > 1`. Paginated server-side at **20/page** (`GalleryPageSize` const in `SightingController`); `Gallery(int page = 1)` calls `GetSightingsPageAsync`; `SightingGalleryViewModel` carries `CurrentPage`/`PageSize`/`TotalCount`/`TotalPages`/`HasPreviousPage`/`HasNextPage`. **Known limitation:** the `filterAll`/`filterMine` JS toggle filters the current page only — server-side filtering across pages is a deferred follow-up. |
| `/Sighting/Details/{id}` | `sightingDetailsContainer` | Root container (page rendered signal) |
| `/Sighting/Details/{id}` | `sightingDetailsImage`, `sightingDetailsUploaderIcon`, `sightingDetailsUploaderName` | Image + uploader |
| `/Sighting/Details/{id}` | `sightingDetailsSpecies`, `sightingDetailsTimestamp`, `sightingDetailsLocation`, `sightingDetailsDescription` | Sighting metadata |
| `/Sighting/Details/{id}` | `sightingDetailsFunFact` | Fun fact. `data-fun-fact-status="ok"` or `"fallback"` |
| `/Sighting/Details/{id}` | `backToGalleryLink` | Back to Gallery |
| `/Sighting` (NotFound) | `sightingNotFoundMessage`, `notFoundBackToGalleryLink` | Not-found view |
| Sightings dropdown | `anidexNavLink` | "My Anidex" (authenticated only) |
| `/anidex` | `anidexEmptyState`, `anidexGrid`, `anidexSpeciesCount` | Page state |
| `/anidex` | `.anidex-entry`, `.anidex-species-name`, `.anidex-rarity-badge`, `.anidex-discovery-count` | Per-species card (entry carries `data-species-name`) |
| `/dashboard` | `accountSettingsLink` | Link to settings |
| `/dashboard/badges` | `currentUserId`, `.badge-card`, `.badge-step-count`, `.badge-card.border-success`, `span.badge.bg-success` | Badges page |
| `/dashboard/settings` | `displayNameInput`, `updateDisplayNameBtn`, `displayNameSuccessMessage`, `notificationPreferencesLink` | Settings page |
| `/dashboard/notification-preferences` | `notificationPreferencesForm`, `saveNotificationPreferencesBtn`, `notificationPreferenceSuccess`, `pref_{NotificationType}` | Preferences form |
| `/notifications` | `markAllReadForm`, `markAllReadBtn`, `deleteAllForm`, `deleteAllBtn`, `notificationsEmptyState` | Notification list |
| `_Layout.cshtml` | `aiCompanionModal`, `aiCompanionForm`, `aiCompanionQuestion`, `aiCompanionSubmitBtn`, `aiCompanionMessages` | AI Companion modal |
| `/Species/Search` | `nameInput`, `clearBtn`, `searchForm`, `searchStatus`, `resultCard`, `resultCounter`, `prevBtn`, `nextBtn` | Wildlife search |
| `/Admin/Reports` | `PageUrlFilter`, `UserSearch`, `DateFilter`, `ShowResolved`, `SortBy` | Filter form inputs |
| `/Admin/Reports` | `.resolution-toggle[data-id]`, `.details-btn[data-id][data-description][data-resolved]` | Per-row controls |
| `/Admin/Reports` | `#reportDetailsModal`, `#modalDescription`, `#modalIsResolved`, `#confirmResolveBtn` | Details modal |
| `/Admin/Manage` | `#promoteForm`, `#demoteForm`, `#lockForm`, `#unlockForm` | Admin action forms |
| `/Admin/Manage` | `.user-search[data-find-locked]`, `.selected-email`, `.modal-password-target` | Lock/Unlock inputs |
| `/Admin/Manage` | `#adminPasswordModal`, `#modalAdminPassword`, `#confirmAuthBtn` | Admin password modal |
| `/Admin/Manage` | `<datalist id="lockList">`, `<datalist id="unlockList">` | Autocomplete lists |

Access-denied detection: `driver.Url` contains `/account/login` (case-insensitive).

---

## PBI Implementation Workflow

**Red/Green/Refactor:** failing test → minimal impl → refactor → commit.

**Commit conventions:**
```
[CSP-XXX] <what was implemented> (TDD)
[CSP-XXX] BDD: <scenario name from .feature file>
```

**Test requirements:** Unit tests for all new/modified services and controllers. At least one Reqnroll scenario per acceptance criterion.

### Pull Request Conventions

- **Base branch:** `dev` (`--base dev`) — never `main`
- **Reviewer:** `jmcshane22`; **Assignees:** `jmcshane22`, `beastmode24jd`; **Draft:** always
- **Labels:** check `gh label list --repo jmcshane22/MonmouthHoldemCapstone`

**`gh pr edit` is broken — use REST API:**
```bash
gh api repos/jmcshane22/MonmouthHoldemCapstone/pulls/{n}/requested_reviewers \
  --method POST --field 'reviewers[]=jmcshane22'
gh api repos/jmcshane22/MonmouthHoldemCapstone/issues/{n}/assignees \
  --method POST --field 'assignees[]=jmcshane22' --field 'assignees[]=beastmode24jd'
gh api repos/jmcshane22/MonmouthHoldemCapstone/issues/{n}/labels \
  --method POST --field 'labels[]=enhancement'
gh pr ready {n} --repo jmcshane22/MonmouthHoldemCapstone --undo  # convert back to draft
```

After every PBI: update this file with new element IDs, service changes, ViewModel properties, Drivers/PageObjects, non-obvious patterns.

---

## CI/CD

**Workflows:** `pr_deploy_and_merge.yml` (on approved non-draft PR → deploy + auto-merge), `deploy.yml` (manual), `test_suite_complete_run.yml` (full suite), `manual_pr_test_run.yml` (pre-approval).

Tests do **not** run on push/PR open. Manual dispatch:

```bash
gh workflow run manual_pr_test_run.yml \
  --repo jmcshane22/MonmouthHoldemCapstone \
  --ref dev \
  --field pr_number=<PR_NUMBER>
```

Poll (takes 10+ minutes):
```bash
gh run list --repo jmcshane22/MonmouthHoldemCapstone \
  --workflow test_suite_complete_run.yml --limit 5
```

---

## Configuration Notes

- Connection string: `DataDb` (both DbContexts)
- Password policy: min 8 chars, digit, upper, lower, non-alphanumeric (e.g. `Capstone26!`)
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
| Acceptance test features | `src/MH.Capstone.Tests.Acceptance/Features/` |
| Acceptance test seeder | `src/MH.Capstone.Tests.Acceptance/Seeding/AcceptanceTestSeeder.cs` |
| Acceptance testing guide | `docs/acceptance_testing.md` |
| Architectural guidelines | `docs/architectural_guidelines.md` |

---

## Database Schema

**Non-obvious constraints:**
- `ApplicationUser.DisplayName`: `nvarchar(50)`, required, defaults to `"UNSET"`; 2–50 chars enforced in service.
- `Sighting.Timestamp` must be past (`[PastDateTime]`). `ImageBuffer` required (1 byte – 2 MB). `SpeciesName` nvarchar(100), required; default `"Unknown"` for migrated rows.
- `Report`: unique filtered index on `(ReportingUserId, ReportedPageUrl)` where `IsResolved = 0`.
- `FixSightingUserIdType` migration uses idempotent raw SQL instead of generated `AlterColumn` — do not regenerate it without restoring the raw SQL (`IF EXISTS` guard on index drop).

**Seeded roles:** `User`, `Admin`. **FK seeding order:** `AspNetRoles` → `AspNetUsers` → `Badge` → `Sighting`/`PersonalBadges`/`Notification`/`Report` → `EmailQueue`.

**Seeded badges:**

| Constant | GUID | Title | Points | BadgeSteps |
|---|---|---|---|---|
| `BadgeId.ProfileBadgeGUID` | `A1B2C3D4-E5F6-4789-8A9B-0C1D2E3F4A5B` | Custom Profile Badge | 10 | 1 |
| `BadgeId.CustomBioBadgeGUID` | `91E7773E-F6D7-457E-911E-8246891D65A2` | Custom Bio Badge | 10 | 1 |
| `BadgeId.FirstSightingBadgeGUID` | `B2C3D4E5-F6A7-4890-9B0C-1D2E3F4B5A6F` | First Sighting Badge | 25 | 1 |
| `BadgeId.SightingNoviceBadgeGUID` | `27857EC5-189E-46E8-BE28-871123607F20` | Sighting Novice | 35 | 5 |
| `BadgeId.SightingStudentBadgeGUID` | `8436745D-C25B-44BF-A0E1-0C87E6122724` | Sighting Student | 50 | 25 |
| `BadgeId.AnidexBeginnerBadgeGUID` | `C3D4E5F6-A7B8-4901-AC1D-2E3F4B5A6F7E` | Anidex Beginner | 35 | 5 |

**`PhotoQualityTier` enum:** `Unknown=0, Low=1, Medium=2, High=3`. Sharpness/luminance **not yet implemented** — `PhotoQualityService` returns `Unknown/0.0/0.0` + real dimensions. Acceptance steps tagged `@photo-quality` throw `NotImplementedException`.

---

## Admin Report Queue

`GET /Admin/Reports` — report triage UI. `wwwroot/js/reportModal.js` handles the details modal and inline resolution toggle.

**Gotchas:**
- `Report.ReportingUserId` is `[NotMapped]` — wrapper around string column `ReportingUserIdentityId`. Filter queries must use `ReportingUserIdentityId`.
- `ReportQueueViewModel.UserSearch` is DisplayName (not id) — controller resolves to id; sentinel `"ID_NOT_FOUND"` when user not found (produces 0 results).
- `SortReports` date filter: `query.Where(r => r.SubmittedAt <= date.Value)` — on or *before* the date. The inline comment claims "on or after" but the code is the source of truth.
- `.resolution-toggle` change listeners bind at page load — won't wire rows added after initial render.

---

## Admin Management Page

`GET /Admin/Manage` — four admin actions (promote, demote, lock, unlock) through a shared password-confirmation modal. `wwwroot/js/manageAccounts.js`.

**Gotchas:**
- Lock/Unlock use DisplayName autocomplete. `.selected-email` hidden field only populates when typed text exactly matches a datalist result (case-insensitive). `showPasswordModal` blocks submission when `.selected-email` is empty.
- Debounce on `.user-search` inputs is 250ms — added because rapid `SendKeys` in Selenium caused out-of-order fetch responses to clobber the hidden email field.

---

## User Profile

`GET /account` (self) and `GET /account/{id:guid}` (other user) — User Profile page, formerly called Account Info. Served by `AccountController.Index` (`src/MH.Capstone.WebApp/Controllers/AccountController.cs`). View: `Views/Account/Index.cshtml`. ViewModel: `Models/AccountViewModel.cs`. `[Authorize]` (whole controller).

**Routing:** one action handles both routes. Missing/empty `id` → current user with `IsAuthenticatedUser = true`. Unknown `id` → `NotFound`. `id` equal to the viewer's own id still renders with `IsAuthenticatedUser = true` (compared via `userFromId.Id == user.Id`). Follow/block state is populated **only** when viewing someone else.

**View zones:**
- Header — title flips between "Your Profile" and "{DisplayName}'s Profile". 80×80 avatar from `ProfileImageUrl`. Status badge (Active/Deactivated). Bio.
- Stub list — "Recent Badges", "Recent Clubs", "Recent Sightings" are empty `<li>` placeholders. **`Total points:` value is commented out** in the view (`@* @Model.Points *@`), so points never render despite the VM populating them. `AccountController.Index` carries a `// Sprint 7: Add bio field, point count, recent Badges, and Clubs to the User Profile` TODO marker for the planned fill-in.
- Social actions (CSP-187) — Follow/Unfollow + Block/Unblock forms, shown only when `!IsAuthenticatedUser`. POSTs go to `UserController` (`/user/{id}/follow|unfollow|block|unblock`), each of which redirects back to `Account/Index?id={id}`. Errors surface via `TempData["FollowError"]` / `TempData["BlockError"]` as inline alerts.
- Edit button — shown only when `IsAuthenticatedUser`, links to `Dashboard/Settings`.

**`AccountViewModel`:** `Id`, `Username`, `DisplayName`, `Points` (int?), `IsDeactivated`, `ProfileImageUrl`, `IsAuthenticatedUser`, `Bio` (default placeholder `"Enter a unique profile bio."`), `IsFollowedByCurrentUser`, `IsBlockedByCurrentUser`. The `ApplicationUser`-based ctor does **not** set the follow/block flags — `AccountController.Index` populates them via `IFollowService.IsFollowingAsync` / `IBlockService.IsBlockedAsync`, but only when viewing another user.

**Gotchas:**
- `Total points:` row label renders without a value — `@Model.Points` is commented out in `Index.cshtml`. Anything that asserts on point display must un-comment that expression first.
- Follow/Block POSTs live on `UserController`, not `AccountController`. Both controllers carry `[Authorize]`, but they each separately depend on `IFollowService` + `IBlockService` — DI must register both for the profile page to function end-to-end.
- `IsFollowedByCurrentUser` / `IsBlockedByCurrentUser` stay `false` when viewing your own profile, which is also the condition that hides the entire social-actions block — don't repurpose those flags for any self-view logic.

**Element IDs (`Views/Account/Index.cshtml`):** `profileSocialActions`, `followButton`, `unfollowButton`, `blockButton`, `unblockButton`, `profileFollowError`, `profileBlockError`, `accountEditBtn`.

---

## Test Seed Data Guidance

- `Sighting.ImageBuffer` required — use `new byte[] { 0x01 }`
- `Sighting.Timestamp` must be past — use `DateTimeOffset.UtcNow.AddDays(-N)`
- `Report` unique filtered index — stagger `IsResolved` or use different URLs per user
- Users need `NormalizedEmail`, `NormalizedUserName` (`.ToUpper()`), hashed password via `PasswordHasher<ApplicationUser>`
- `SightingUploadViewModel` requires `DeviceTimezone` (default `"America/Los_Angeles"`); `ToDataModel()` converts to UTC

**`SightingDetailsViewModel`:** Null/whitespace `funFact` → `IsFunFactFallback = true` + fallback message ("Fun facts about this animal aren't available right now."). View sets `data-fun-fact-status="fallback"` when true.

**CSP-184 acceptance seeder:** Seeds `SightingNoviceBadge` (`BadgeSteps = 5`). `SightingStudentBadge` and `AnidexBeginnerBadge` are **not yet** in the acceptance seeder — add them before writing tests for those badges. `[BeforeScenario("badge")]` calls `ResetSeedDataAsync()` so Alex starts at exactly 4 seeded sightings — without this, sightings submitted by other features push him past the 5-sighting threshold.

**Seeded species (CSP-142):** Alex: `Great Blue Heron` ×2 + `Bald Eagle` ×1 (Anidex = 2 entries). Lily: `Wolverine`, `Peregrine Falcon`, `River Otter`, `Roosevelt Elk`, `Coyote`. Don't rename without updating `Features/CSP-142.feature`.

**CSP-172 fallback sighting:** Alex has sighting `a1000000-0000-0000-0000-000000000004` with `SpeciesName = "Mystery Critter Z"` — deliberately unmatchable. Don't rename without updating `CSP172StepDefinitions.AlexUnresolvedSpeciesSightingId`.

---

## Jira PBI Guidelines

Full template and checklist: `docs/pbi_guidelines.md`. Every acceptance criterion needs a Gherkin scenario. Append to every created/modified PBI:

```
---
AI Agent <Agent Name> assisted in the creation and/or modification of this PBI.
```
