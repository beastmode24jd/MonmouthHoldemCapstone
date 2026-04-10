# Codebase Audit - Monmouth Hold'em Capstone (WildlifeAID)

## Project Summary

**What it is:** A gamified wildlife observation web app built for Western Oregon University's Academic Excellence Showcase (May 28, 2026). Users log wildlife sightings, earn points based on rarity, collect badges, and compete on a leaderboard.

**Team:** Monmouth Hold'em (Marquis Bowles, JD McShane, Arin Porter, Pedro Govea)
**Repository:** jmcshane22/MonmouthHoldemCapstone
**Current Phase:** Sprint 4 of 8 (March 31 - April 14, 2026)

---

## Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Runtime | .NET | 9.0 |
| Framework | ASP.NET Core MVC | 9.0.x |
| ORM | Entity Framework Core | 9.0.13 |
| Database | Azure SQL Server | - |
| Auth | ASP.NET Core Identity | 9.0.12 |
| Frontend CSS | Bootstrap | 5.3 |
| Frontend JS | Vanilla JS (planned: Leaflet.js for maps) |
| Testing | NUnit 4.4, Moq 4.20, Reqnroll 3.3.4, Selenium 4.41 |
| Cloud | Microsoft Azure (App Service, SQL, Communication Services) |
| CI/CD | GitHub Actions |
| External API | API Ninjas (Animals endpoint) |

---

## Architecture

### Pattern: Layered MVC with Repository Pattern

```
MH.Capstone.WebApp (Presentation)
  ├── Controllers (9 controllers)
  ├── Views (Razor, organized by feature)
  ├── Models (ViewModels for data binding)
  ├── wwwroot (static assets: CSS, JS, images)
  └── Program.cs (DI registration, middleware)

MH.Capstone.Domain (Business Logic + Data Access)
  ├── DataModels (EF Core entities)
  ├── DataAccess (DbContexts, generic Repository<T>)
  ├── Services (business logic, interfaces in /Abstraction)
  ├── ApiContracts (external API DTOs)
  ├── Constants (BadgeIds, feature flags)
  ├── Migrations (20+ EF Core migrations)
  └── Tools (validators, extensions)
```

### Test Projects
- **WebApp.Tests.Unit** - Controller + ViewModel tests (NUnit)
- **Domain.Tests.Unit** - Service layer tests (NUnit + Moq)
- **Tests.Integration** - Leaderboard, Reports, Gallery integration tests
- **Tests.Acceptance** - BDD with Reqnroll (Leaderboard, SightingsMap features)
- **Tests.SharedInternals** - Shared test utilities (fake images, random data, SQL exception builder)

---

## Core Data Models

| Entity | Key Fields | Notes |
|--------|-----------|-------|
| **ApplicationUser** | ProfileImage (byte[]), IsDeactivated, Points, Bio, LoginStreak, LastLogin | Extends IdentityUser |
| **Sighting** | Lat/Lng (decimal 9,6), Timestamp (DateTimeOffset), ImageBuffer (byte[]), Description | JPG/PNG only, max 2MB, 7-day map window |
| **Badge** | Title, Description, PointValue, BadgeIcon | 3 seeded badges with fixed GUIDs |
| **UserBadge** | UserId, BadgeId, BadgeEarned | Join table |
| **Notification** | Title (50), Message (250), IsRead, SentAt | In-app notifications |
| **Report** | ReportedPageUrl, Reason, Description, IsResolved | Unique constraint: (UserId, Url) per unresolved |
| **EmailQueue** | Recipient, Subject, HtmlBody, IsSent, Attempts | Background dispatch with retry |

---

## Scoring System

- **Base points:** 10 per sighting
- **Rarity multipliers** (based on global sighting count):
  - Mythic (<=5 sightings): 5.0x = 50 pts
  - Rare (<=50 sightings): 2.0x = 20 pts
  - Common (>50 sightings): 1.0x = 10 pts
- **Login streak bonus:** 1.5x multiplier if streak active (within 30 days)
- **Badges:**
  - Profile Image Badge: 10 pts
  - Custom Bio Badge: 10 pts
  - First Sighting Badge: 25 pts

---

## API Routes

| Route | Controller | Auth | Purpose |
|-------|-----------|------|---------|
| `/` | Home | No | Landing page |
| `/about`, `/privacy` | Home | No | Info pages |
| `/account/login` | Account | No | Login |
| `/account/register` | Account | No | Registration |
| `/account/{guid}` | Account | No | Public profile |
| `/account/deactivate` | Account | Yes | Self-deactivate |
| `/account/reactivate` | Account | No | Reactivate |
| `/account/forgot-password` | Account | No | Password reset |
| `/dashboard` | Dashboard | Yes | User dashboard, badges, stats |
| `/dashboard/UploadImage` | Dashboard | Yes | Profile image upload |
| `/dashboard/UpdateBio` | Dashboard | Yes | Update bio |
| `/notifications` | Dashboard | Yes | View notifications |
| `/notifications/pending-count` | Dashboard | Yes | Unread count (JSON) |
| `/Sighting/Upload` | Sighting | Yes | Log a sighting |
| `/Sighting/Gallery` | Sighting | Yes | User's sightings |
| `/Map` | Map | Yes | Interactive sightings map |
| `/Map/Sightings` | Map | Yes | Sightings within bounds (JSON) |
| `/Map/SightingImage/{id}` | Map | Yes | Sighting image |
| `/leaderboard` | Leaderboard | No | Rankings (paginated, 30/page) |
| `/animal/search` | Species | No | Animal search (Ninjas API) |
| `/Report/Submit` | Report | Yes | Submit abuse report |
| `/admin/manage` | Admin | Admin | User management |
| `/uat/emailer` | Home | Feature-flagged | Email test endpoint |

---

## Data Flow: Sighting Upload

1. User visits `/Sighting/Upload` (GET) - controller reads timezone from cookie
2. User submits form with coordinates, timestamp, description, image
3. **SightingController.Upload()** validates ModelState, image type (JPG/PNG), size (<=2MB)
4. **SightingsService.CreateSightingAsync()** saves to DB
5. **ScoringService** counts global sightings, calculates rarity multiplier, checks login streak
6. User points updated in database
7. **NotificationService** sends in-app notification
8. **BadgeService** awards FirstSightingBadge (if first time) with streak multiplier
9. Redirect to Dashboard with success TempData message

---

## Key Architectural Patterns

- **Repository Pattern:** Generic `Repository<TEntity, TDbContext>` with async CRUD + predicate filtering
- **Dependency Injection:** All services registered as Scoped in Program.cs
- **Service Abstraction:** Every service has an interface in `/Services/Abstraction/`
- **Caching Proxy:** `ApiCallerCachingProxy` wraps `ExternalApiCaller` for transparent response caching
- **Background Service:** `EmailDispatcherService` runs continuously for async email dispatch with retry
- **Feature Flags:** `UseRealEmailerService`, `EnableEmailTestEndpoint`, `ExposeDetailedApiCacheOnUi`
- **CSRF Protection:** `[ValidateAntiForgeryToken]` on POST endpoints
- **Role-Based Auth:** Admin role for user management

---

## CI/CD Pipeline

### build_test_ci.yml (PR + manual trigger)
1. **validate-ef:** Checks for missing EF Core migrations
2. **buildtest:** Restore -> Build (Release) -> Test -> Publish -> Create migration bundle -> Upload artifacts

### deploy.yml (push to main/dev)
1. Calls build_test_ci with deploy=true
2. Sets environment (main=azure_prod, dev=azure_staging)
3. Creates GitHub Release (prerelease for dev)
4. Deploys to Azure App Service via OIDC
5. Runs EF Core migration bundle against Azure SQL

**Versioning:** YYYY.M.{run_number}.{run_attempt}

---

## CRITICAL SECURITY VULNERABILITIES

### 1. HARDCODED DATABASE CREDENTIALS (CRITICAL)

**Files affected:**
- `src/MH.Capstone.WebApp/appsettings.Development.json` (line ~8-10)
- `src/MH.Capstone.Tests.Acceptance/StepDefinitions/LeaderboardSteps.cs` (line ~367)

**Exposed:** Azure SQL Server hostname, database name, username (`mbowles23`), and password in plaintext.

**Action Required:**
1. **Immediately rotate** the database password in Azure SQL
2. Remove hardcoded credentials from acceptance tests - use environment variables or user secrets
3. Verify `appsettings.Development.json` is properly gitignored (it's in .gitignore but may have been committed previously)
4. Consider running `git filter-repo` or BFG to scrub credentials from git history
5. Add a pre-commit hook or CI check to scan for secrets (e.g., TruffleHog, GitLeaks)

### 2. Images Stored as byte[] in Database

Storing images directly in the database (profile images, sighting images) as `byte[]` columns is a scalability concern. As the user base grows, database size and query performance will degrade. Consider Azure Blob Storage for image storage with URL references in the DB.

### 3. AllowedHosts Set to Wildcard

`appsettings.json` has `"AllowedHosts": "*"` which allows requests from any host. In production, this should be restricted to the actual domain.

---

## Technical Debt

### High Priority
- **Hardcoded connection strings** in test code (security + maintainability)
- **Images in database** instead of blob storage (scalability bottleneck)
- **No pagination on notifications** - could grow unbounded
- **Mixed authentication package versions** - Domain layer references `Microsoft.AspNetCore.Authentication 2.3.9` alongside `.NET 9.0` packages

### Medium Priority
- **No rate limiting** on API endpoints (login, sighting upload, report submission)
- **No input sanitization** documented for user-generated content (descriptions, bios) beyond model validation
- **Timezone handling complexity** - IANA to Windows timezone ID conversion with fallback; could cause edge cases
- **No health check endpoint** for Azure monitoring
- **Leaderboard queries all active users** - will need indexing/caching as user count grows

### Low Priority
- **jQuery included but not used** for development (only Bootstrap dependency)
- **No client-side JS framework** - vanilla JS may become hard to maintain as features grow
- **No API versioning** for the JSON endpoints
- **Email sender address** is a raw Azure Communication Services GUID domain

---

## Critical Knowledge for Maintenance

### To Run Locally
1. Install .NET 9.0 SDK (v9.0.309)
2. Set up `appsettings.Development.json` with local/dev connection strings (DO NOT commit)
3. Run EF Core migrations: `dotnet ef database update`
4. Run: `dotnet run --project src/MH.Capstone.WebApp`

### To Add a New Feature
1. Create feature branch named after Jira ticket (e.g., `CSP-XXX`)
2. Add data models in `MH.Capstone.Domain/DataModels/`
3. Add migration: `dotnet ef migrations add MigrationName`
4. Add service interface in `/Services/Abstraction/` and implementation in `/Services/`
5. Register service in `Program.cs` (or `ProgramEntryExtensions.cs`)
6. Add controller actions and views
7. Write unit tests (NUnit + Moq)
8. PR with template from `/docs/pr-templates/`

### To Add a New Badge
1. Add GUID constant in `Constants/BadgeId.cs`
2. Add seed data in `ApplicationDbContextSeeding.cs`
3. Create migration for the seed
4. Add award logic in `BadgeService.cs`
5. Trigger award from relevant controller/service action

### Key Configuration
- **Feature flags** are in `appsettings.json` and read via `IConfiguration`
- **Connection strings** should come from Azure App Settings in production (GitHub Secret: `AZUREAPPSERVICE_DBCONNSTR`)
- **Ninjas API key** configured under `Api:External:Ninjas:ApiKey`

### Database
- **Two DbContexts:** `ApplicationDbContext` (main data + Identity) and `CacheDbContext` (API response cache)
- **Lazy loading** is enabled via EF Core Proxies - navigation properties must be `virtual`
- **All timestamps** stored as UTC `DateTimeOffset`, converted to local for display

### Team Schedule
- Mon/Wed/Fri: Standup meetings
- Tuesday: Tech advisor meeting
- Sprint cycle: 2 weeks
- Showcase deadline: May 28, 2026
