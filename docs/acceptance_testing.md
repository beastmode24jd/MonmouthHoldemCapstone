# Acceptance Testing Guide

## Competitive Wildlife Scavenger App

---

## Overview

Acceptance tests in this project are written in **Gherkin** (`.feature` files) and executed by **Reqnroll** against a real, running instance of `MH.Capstone.WebApp`. Selenium drives a Chrome browser through the full UI, making these tests as close to real user behaviour as possible.

The test project (`MH.Capstone.Tests.Acceptance`) starts and stops the WebApp automatically when the test run begins and ends — no manual startup is required.

---

## Tech Stack

| Concern | Technology | Version |
|---|---|---|
| BDD / Test Runner | Reqnroll (Reqnroll.NUnit) | 3.3.4 |
| Unit Test Adapter | NUnit + NUnit3TestAdapter | 4.4.0 / 6.1.0 |
| Browser Automation | Selenium WebDriver | 4.41.0 |
| Browser Driver | Selenium ChromeDriver (Chrome 147) | 147.0.7727.5600 |
| Assertions | FluentAssertions | 8.9.0 |
| DI Container (Reqnroll) | Reqnroll.Microsoft.Extensions.DependencyInjection | 3.3.4 |
| WebApp Host (in-process) | `Microsoft.AspNetCore.App` framework reference | .NET 9 |
| Configuration | `Microsoft.Extensions.Configuration` (JSON + EnvVars) | via framework ref |

---

## Project Structure

```
MH.Capstone.Tests.Acceptance/
├── Configuration/
│   ├── AcceptanceTestConfiguration.cs   # Locates WebApp dir and loads config
│   └── AcceptanceTestSettings.cs        # Settings POCO (BaseUrl, HeadlessSelenium)
├── Drivers/                             # Selenium action abstractions
│   ├── AuthenticationDriver.cs
│   ├── DashboardDriver.cs
│   └── SightingsDriver.cs
├── Features/                            # Gherkin feature files
│   ├── CSP-53.feature
│   └── SightingsMap.feature
├── Hooks/
│   └── GlobalHooks.cs                   # BeforeTestRun / AfterTestRun lifecycle
├── PageObjects/                         # Lazy-loaded Selenium page element wrappers
│   ├── LoginPageObject.cs
│   └── SightingsUploadPageObject.cs
├── StepDefinitions/                     # Gherkin step implementations
│   ├── CSP53StepDefinitions.cs
│   └── SightingsMapSteps.cs
├── TestDependencySetup.cs               # Reqnroll DI registration
└── TestWebAppHost.cs                    # In-process Kestrel startup / shutdown
```

---

## How the WebApp Is Started

When `dotnet test` runs the acceptance project, Reqnroll's `[BeforeTestRun]` hook fires `GlobalHooks.BeforeTestRun()`, which:

1. Loads `AcceptanceTestSettings` from the WebApp's config hierarchy (see below).
2. Creates a single shared **ChromeDriver** instance (headless by default).
3. Calls `TestWebAppHost.StartAsync()`, which starts `MH.Capstone.WebApp` **in-process** on a real Kestrel listener bound to `AcceptanceTesting:BaseUrl`.

After all scenarios complete, `GlobalHooks.AfterTestRun()` stops the WebApp and closes the browser.

Because the WebApp runs in the same process, no separate terminal or `dotnet run` command is needed.

---

## Prerequisites

The following must be in place on any machine that runs the acceptance tests.

### 1. .NET 9 SDK

```
dotnet --version  →  9.x.xxx
```

### 2. Google Chrome

Chrome **version 147** must be installed. The `Selenium.WebDriver.ChromeDriver` NuGet package pins to a matching ChromeDriver version. If your Chrome is a different major version, update the `Selenium.WebDriver.ChromeDriver` package version in the `.csproj` to match.

### 3. Trusted HTTPS Development Certificate

The WebApp starts on `https://` by default. The local dev certificate must be trusted:

```bash
dotnet dev-certs https --trust
```

> Alternatively, change `AcceptanceTesting:BaseUrl` to `http://localhost:5001` in your local override file (see configuration below) to avoid TLS entirely. Chrome is configured with `--allow-insecure-localhost` so self-signed certs are silently accepted.

### 4. SQL Server / LocalDB

The acceptance tests use a **real database**. SQL Server LocalDB (included with Visual Studio) is the recommended local setup:

```
Data Source=(localdb)\MSSQLLocalDB
```

> See the database section below for the dedicated test database recommendation.

### 5. EF Core Migrations Applied

Before the first run, apply migrations to the acceptance test database:

```bash
# From the repository root
dotnet ef database update \
  --project src/MH.Capstone.Domain \
  --startup-project src/MH.Capstone.WebApp \
  --connection "Data Source=(localdb)\MSSQLLocalDB;Database=WAID_AcceptanceDb;..."
```

---

## Configuration

Configuration for the acceptance tests is layered, later sources override earlier ones:

| Layer | File | Committed? | Purpose |
|---|---|---|---|
| 1 | `MH.Capstone.WebApp/appsettings.json` | Yes | Base defaults and placeholder secrets |
| 2 | `MH.Capstone.WebApp/appsettings.Acceptance.json` | Yes | Acceptance environment defaults (feature flags, shared settings) |
| 3 | `MH.Capstone.WebApp/appsettings.Acceptance.Local.json` | **No** (gitignored) | Per-developer overrides |
| 4 | Environment variables | N/A | CI/CD overrides |

### Creating Your Local Override File

Copy the example template and fill in your values:

```
src/MH.Capstone.WebApp/appsettings.Acceptance.Local.json.example
                                         ↓ copy to
src/MH.Capstone.WebApp/appsettings.Acceptance.Local.json
```

The local file is already covered by the repository's `.gitignore` rule (`appsettings.*.json`) and will never be committed.

### Key Configuration Values

All under the `AcceptanceTesting` JSON section:

| Key | Default | Description |
|---|---|---|
| `AcceptanceTesting:BaseUrl` | `https://localhost:5001` | URL Kestrel binds to; URL Selenium navigates to |
| `AcceptanceTesting:HeadlessSelenium` | `true` | Set to `false` to watch Chrome during test runs |

Connection strings, API keys, and feature flags follow the same WebApp appsettings structure as normal development.

### CI/CD Configuration

In GitHub Actions (or any CI system), supply secrets as environment variables. The `AddEnvironmentVariables()` call picks them up automatically:

```yaml
env:
  ConnectionStrings__DataDb: ${{ secrets.ACCEPTANCE_DB_CONNECTION }}
  Api__External__Ninjas__ApiKey: ${{ secrets.NINJA_API_KEY }}
  AcceptanceTesting__BaseUrl: "https://localhost:5001"
  AcceptanceTesting__HeadlessSelenium: "true"
```

> Note: environment variable key separators use double underscores (`__`) to represent JSON nesting.

---

## Database Recommendation

> **A dedicated acceptance test database is strongly recommended.**

The acceptance test suite is designed to automate browser interactions against real application state. This means:

- The WebApp **seeds data on startup** (`ApplicationDbContextSeeding`) and will insert rows (badges, roles) every time the host starts.
- Future test infrastructure will include **`TestWebAppHost.ResetSeedData()`**, which will delete and re-seed rows between scenarios to guarantee a clean baseline.
- Sightings, users, reports, and notifications created during test scenarios will persist in the database unless explicitly cleaned up.

Running acceptance tests against a shared development or production database risks:

- **Polluting real data** with test records.
- **Test flakiness** caused by leftover state from a prior run.
- **Reset operations deleting real data** when `ResetSeedData` is implemented.

### Recommended Setup

| Environment | Database Name | Purpose |
|---|---|---|
| Developer machine | `WAID_AppDataDb` | Day-to-day development |
| Acceptance tests | `WAID_AcceptanceDb` | Dedicated acceptance test target (separate from dev DB) |
| CI/CD | Ephemeral or dedicated CI DB | Created fresh per pipeline run if possible |

Set `ConnectionStrings:DataDb` in `appsettings.Acceptance.Local.json` to point at `WAID_AcceptanceDb`.

---

## Test Personas

Test personas are fixed user accounts seeded into the acceptance test database. Using central, named personas ensures Gherkin scenarios stay readable and consistent across feature files — rather than inline credentials scattered throughout step definitions.

> **The persona accounts below are for acceptance testing only. They should exist in the dedicated acceptance test database and never in production.**

Credentials referenced in Gherkin scenarios (e.g. `Given user Alpha is logged in`) resolve to the accounts defined here.

---

### Persona Accounts

| Persona | Gherkin Alias | Email | Password | Roles | Purpose |
|---|---|---|---|---|---|
| Alex | `user Alex is logged in` | `alex@test.com` | `Capstone26!` | User | Standard authenticated user; covers all user-role permission cases |
| Patricia | `user Patricia is logged in` | `patricia@test.com` | `Capstone26!` | Admin, User | Covers admin-only access, admin UI, and elevated permission cases |
| James | `an unauthenticated user` | _(no account)_ | _(no account)_ | None | Represents an anonymous/unauthenticated visitor; covers access-denied and login-redirect cases |
| Lily | `user Lily is logged in` | `lily@test.com` | `Capstone26!` | User | Second standard user; covers multi-user interaction cases (e.g. leaderboard comparisons, viewing another user's content) |

---

#### Alex — Standard User

- **Email:** `alex@test.com`
- **Password:** `Capstone26!`
- **Roles:** `User`
- **Purpose:** The default persona for any scenario that requires a logged-in user without elevated permissions. Use Alex whenever the scenario is testing a user-facing feature and role/permission distinctions are not the focus.

---

#### Patricia — Admin User

- **Email:** `patricia@test.com`
- **Password:** `Capstone26!`
- **Roles:** `Admin`, `User`
- **Purpose:** Use Patricia for any scenario that tests admin-only functionality (e.g. the Admin management panel, resolving reports, user moderation). Also suitable for verifying that admin users retain access to all standard user features.

---

#### James — Unauthenticated / Anonymous User

- **Email:** _(no account)_
- **Password:** _(no account)_
- **Roles:** None
- **Purpose:** James represents any visitor who is not logged in. Use James for scenarios that verify access control — confirming that protected pages redirect to the login page and that unauthenticated users cannot perform authenticated actions.

---

#### Lily — Second Standard User

- **Email:** `lily@test.com`
- **Password:** `Capstone26!`
- **Roles:** `User`
- **Purpose:** A second standard user, distinct from Alex. Use Lily in scenarios that require two separate authenticated users to be present — for example, verifying leaderboard rankings between users, testing that one user cannot modify another user's sightings, or checking notification isolation.

---

## Running the Tests

```bash
# Run only the acceptance tests
dotnet test src/MH.Capstone.Tests.Acceptance/MH.Capstone.Tests.Acceptance.csproj

# Run all tests in the solution
dotnet test src/MH.Capstone.sln

# Show Chrome browser window during the run (useful for debugging)
# Set AcceptanceTesting:HeadlessSelenium = false in your local override file, then:
dotnet test src/MH.Capstone.Tests.Acceptance/MH.Capstone.Tests.Acceptance.csproj
```

---

## Writing New Scenarios

1. Add a `.feature` file in `Features/` using Gherkin syntax.
2. Reference persona names (defined above) for any steps that require a logged-in user.
3. Implement step definitions in `StepDefinitions/`, injecting the relevant `Driver` classes.
4. Add new `Driver` or `PageObject` classes as needed; inject `AcceptanceTestSettings` for the base URL.

### Example

```gherkin
Feature: Sightings Upload [CSP-53]

  Scenario: Logged-in user can navigate to the upload page
    Given user Alpha is logged in
    When user navigates to the sightings upload page
    Then the upload form is displayed
```

Drivers and page objects should never hardcode URLs. Always use `settings.BaseUrl` from the injected `AcceptanceTestSettings`.
