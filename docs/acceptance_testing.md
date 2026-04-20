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
│   └── AcceptanceTestSettings.cs        # Settings POCO (BaseUrl, WebAppContentRoot, HeadlessSelenium)
├── Drivers/                             # Selenium action abstractions (registered in TestDependencySetup)
│   ├── AuthenticationDriver.cs          # Login, logout helpers
│   ├── DashboardDriver.cs               # Dashboard navigation and interactions
│   ├── EmailVerificationDriver.cs       # Email confirmation flow (register + verify)
│   ├── PasswordResetDriver.cs           # Forgot password + reset flow
│   ├── SightingGalleryDriver.cs         # Gallery filter and card interactions
│   ├── SightingsDriver.cs               # Sighting submission
│   └── WildlifeSearchDriver.cs          # Species / Anidex search
├── Features/                            # Gherkin feature files (.feature)
│   ├── CSP-26.feature                   # Password reset UI checks
│   ├── CSP-42.feature                   # Profile customization
│   ├── CSP-47.feature                   # Account deactivation
│   ├── CSP-52.feature                   # Sightings map
│   ├── CSP-53.feature                   # Sightings upload
│   ├── CSP-58.feature                   # Sightings gallery
│   ├── CSP-96.feature                   # Email verification
│   ├── CSP-97.feature                   # Leaderboard
│   ├── CSP-101.feature                  # User reporting
│   ├── CSP-120.feature                  # AI companion
│   ├── CSP-133.feature                  # Password reset (full flow)
│   ├── CSP-134.feature                  # Registration email verification
│   └── SightingsMap.feature             # Sightings map (legacy)
├── Helpers/
│   └── WebDriverExtensions.cs           # WaitUntil, WaitForElement, WaitForDocumentReady
├── Hooks/
│   ├── FailureHooks.cs                  # Screenshot on scenario failure
│   └── Startup.cs                       # BeforeTestRun / AfterTestRun / BeforeScenario lifecycle
├── PageObjects/                         # Lazy-loaded Selenium page element wrappers
│   ├── ForgotPasswordPageObject.cs
│   ├── LoginPageObject.cs
│   ├── ResetPasswordPageObject.cs
│   ├── SightingGalleryPageObject.cs
│   ├── SightingsUploadPageObject.cs
│   └── WildlifeSearchPageObject.cs
├── Seeding/
│   └── AcceptanceTestSeeder.cs          # Full wipe + re-seed of WAID_AcceptanceDb at test run start
├── StepDefinitions/                     # Gherkin step implementations
│   ├── CSP-26Steps.cs
│   ├── CSP-42Steps.cs
│   ├── CSP47AccountDeactivationSteps.cs
│   ├── CSP52SightingsMapSteps.cs
│   ├── CSP53StepDefinitions.cs
│   ├── CSP58StepDefinitions.cs
│   ├── CSP96StepDefinitions.cs
│   ├── CSP97StepDefinitions.cs
│   ├── CSP101StepDefinitions.cs
│   ├── CSP120StepDefinitions.cs
│   ├── CSP133StepDefinitions.cs
│   ├── CSP134StepDefinitions.cs
│   └── SightingsMapSteps.cs
├── TestDependencySetup.cs               # Reqnroll DI registration ([ScenarioDependencies])
├── TestOutputLoggerProvider.cs          # Routes ILogger output to NUnit TestContext.Out
└── TestWebAppHost.cs                    # In-process Kestrel startup / shutdown / seed reset
```

---

## How the WebApp Is Started

When `dotnet test` runs the acceptance project, Reqnroll's `[BeforeTestRun]` hook fires `Startup.BeforeTestRun()`, which:

1. Loads `AcceptanceTestSettings` from the WebApp's config hierarchy (see below).
2. Creates a single shared **ChromeDriver** instance (headless by default).
3. Calls `TestWebAppHost.StartAsync()`, which starts `MH.Capstone.WebApp` **in-process** on a real Kestrel listener bound to `AcceptanceTesting:BaseUrl`.

`Startup.BeforeScenario()` navigates the shared browser to `about:blank` before each scenario, providing a clean starting state without the cost of relaunching Chrome.

After all scenarios complete, `Startup.AfterTestRun()` stops the WebApp and closes the browser.

---

## Prerequisites

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

> Alternatively, change `AcceptanceTesting:BaseUrl` to `http://localhost:5001` in your local override file to avoid TLS entirely. Chrome is configured with `--allow-insecure-localhost` so self-signed certs are silently accepted.

### 4. SQL Server / LocalDB

The acceptance tests use a **real database**. SQL Server LocalDB (included with Visual Studio) is the recommended local setup:

```
Data Source=(localdb)\MSSQLLocalDB
```

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

### Key Configuration Values

All under the `AcceptanceTesting` JSON section:

| Key | Default | Description |
|---|---|---|
| `AcceptanceTesting:BaseUrl` | `https://localhost:5001` | URL Kestrel binds to; URL Selenium navigates to |
| `AcceptanceTesting:HeadlessSelenium` | `true` | Set to `false` to watch Chrome during test runs |

### WSL2 Note

When running acceptance tests from WSL2, pass the headless flag as an environment variable to avoid a display error:

```bash
AcceptanceTesting__HeadlessSelenium=true dotnet test src/MH.Capstone.Tests.Acceptance/...
```

### CI/CD Configuration

Supply secrets as environment variables. The `AddEnvironmentVariables()` call picks them up automatically:

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

- **`AcceptanceTestSeeder.SeedAsync`** runs at test-run startup via `TestWebAppHost.StartAsync()`. It wipes all application table rows (DELETE, not DROP) and inserts a complete, deterministic fixture set.
- `TestWebAppHost.ResetSeedDataAsync()` can be called mid-run from `[BeforeScenario]` hooks to restore a clean baseline between scenarios (required if a scenario mutates the database in a way that would affect subsequent scenarios).
- Sightings, users, reports, and notifications created during test scenarios that do **not** clean up after themselves will persist until the next full re-seed.

Running acceptance tests against a shared development or production database risks polluting real data.

### Recommended Setup

| Environment | Database Name | Purpose |
|---|---|---|
| Developer machine | `WAID_AppDataDb` | Day-to-day development |
| Acceptance tests | `WAID_AcceptanceDb` | Dedicated acceptance test target (separate from dev DB) |
| CI/CD | Ephemeral or dedicated CI DB | Created fresh per pipeline run if possible |

Set `ConnectionStrings:DataDb` in `appsettings.Acceptance.Local.json` to point at `WAID_AcceptanceDb`.

---

## Test Personas

All personas are seeded by `AcceptanceTestSeeder`. Using central, named personas ensures Gherkin scenarios stay readable and consistent. The password for all seeded personas is **`Capstone26!`**.

> James has no database account — he represents any unauthenticated visitor and is referenced in step definitions by simply not logging in.

| Persona | Email | Roles | Points | Purpose |
|---|---|---|---|---|
| **Alex** | `alex@test.com` | User | 75 | Standard authenticated user. Default persona for any scenario that requires a logged-in user. Has 3 sightings, 1 badge, mix of read/unread notifications. |
| **Patricia** | `patricia@test.com` | Admin, User | 0 | Admin persona. Use for admin-only pages, report moderation, and elevated-permission scenarios. No sightings (keeps admin state clean). |
| **Lily** | `lily@test.com` | User | 200 | Second standard user. Leaderboard rank #1. Use for multi-user scenarios, leaderboard comparisons, viewing another user's content. Has 5 sightings, all 3 badges. |
| **James** | _(no account)_ | None | — | Unauthenticated visitor. Represents access-denied and login-redirect cases. |

### Stable GUIDs

`AcceptanceTestSeeder` exposes stable GUIDs for step definitions that need to reference specific records by ID:

```csharp
AcceptanceTestSeeder.AlexUserId     // aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
AcceptanceTestSeeder.PatriciaUserId // bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb
AcceptanceTestSeeder.LilyUserId     // cccccccc-cccc-cccc-cccc-cccccccccccc
```

---

## Writing Step Definitions

### Required structure

Every step definition class must follow this structure:

```csharp
[Binding]
[Scope(Tag = "your-feature-tag")]    // Always scope to a tag — prevents step ambiguity
[ExcludeFromCodeCoverage]
public class YourFeatureSteps
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly AcceptanceTestSettings _settings;
    private readonly AuthenticationDriver _authDriver;

    private string BaseUrl => _settings.BaseUrl.TrimEnd('/');

    public YourFeatureSteps(IWebDriver driver, WebDriverWait wait,
        AcceptanceTestSettings settings, AuthenticationDriver authDriver)
    {
        _driver   = driver;
        _wait     = wait;
        _settings = settings;
        _authDriver = authDriver;
    }
}
```

### DI injection — what to inject

All of the following are registered in `TestDependencySetup` and available via constructor injection:

| Type | Registration | Use for |
|---|---|---|
| `IWebDriver` | Singleton | Raw Selenium interactions when no driver class covers the need |
| `WebDriverWait` | Singleton (15 s timeout) | Explicit waits (`_wait.Until(...)`) |
| `AcceptanceTestSettings` | Singleton | `BaseUrl`, `WebAppContentRoot` |
| `AuthenticationDriver` | Transient | Login, logout |
| `DashboardDriver` | Transient | Dashboard navigation |
| `EmailVerificationDriver` | Transient | Email confirmation / registration flow |
| `PasswordResetDriver` | Transient | Forgot password / reset password flow |
| `SightingGalleryDriver` | Transient | Gallery filter and card interactions |
| `SightingsDriver` | Transient | Sighting submission |
| `WildlifeSearchDriver` | Transient | Species catalog search |

> **Registering a new Driver:** Add `services.AddTransient<YourNewDriver>()` in `TestDependencySetup.CreateServices()`. Reqnroll does **not** auto-discover Driver classes — only step definition classes are auto-discovered.

### Step scoping — always use `[Scope(Tag)]`

Every step definition class must be scoped with `[Scope(Tag = "your-tag")]` matching the tag on the feature file's scenarios. Without scoping, identical or similar step phrases across different feature files cause `AmbiguousMatchException` at runtime.

```gherkin
@deactivation
Scenario: Alex can deactivate her account
    ...
```

```csharp
[Binding]
[Scope(Tag = "deactivation")]   // matches @deactivation tag above
public class CSP47AccountDeactivationSteps { ... }
```

### Shared ChromeDriver — never create your own

The browser is created **once** in `Startup.BeforeTestRun()` and shared across all scenarios via the DI container. Step definitions must inject `IWebDriver` — never create a `new ChromeDriver()`:

```csharp
// CORRECT — injected from DI
public YourSteps(IWebDriver driver, ...) { _driver = driver; }

// WRONG — creates a second browser instance; leaks resources
_driver = new ChromeDriver();
```

### No Thread.Sleep — use extension methods

Never use `Thread.Sleep` for timing. Use the extension methods in `WebDriverExtensions`:

| Method | Use when |
|---|---|
| `_driver.WaitForDocumentReady(timeout)` | After a full page navigation |
| `_driver.WaitForElement(By.Id("id"), timeout)` | Waiting for an element to appear in the DOM |
| `_driver.WaitForElementVisible(By.Id("id"), timeout)` | Waiting for an element to appear and be visible |
| `_driver.WaitUntil(d => ..., timeout)` | Custom condition (returns when lambda is truthy) |
| `_wait.Until(d => ...)` | Injected `WebDriverWait` — same semantics, 15 s default |

### AfterScenario — always scope with a tag

`[AfterScenario]` without a tag runs after **every** scenario in the entire test run. Always restrict it to the tag your step class handles:

```csharp
// CORRECT — only runs after @report scenarios
[AfterScenario("report")]
public void CleanupAfterReport() { ... }

// WRONG — runs after every scenario in every feature file
[AfterScenario]
public void Cleanup() { ... }
```

### Accessing the database from step definitions

When a step needs to query or assert against the database directly, use `AcceptanceTestSettings.WebAppContentRoot` to load the connection string via `ConfigurationBuilder`. Never hardcode connection strings or walk the directory tree manually:

```csharp
private IServiceScope GetServiceScope()
{
    var webAppPath = _settings.WebAppContentRoot;
    var configFile = Path.Combine(webAppPath, "appsettings.Acceptance.json");

    if (!File.Exists(configFile))
        Assert.Ignore("Skipped: appsettings.Acceptance.json not found.");

    var configuration = new ConfigurationBuilder()
        .SetBasePath(webAppPath)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Acceptance.json", optional: false)
        .AddJsonFile("appsettings.Acceptance.Local.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var connectionString = configuration.GetConnectionString("DataDb")
        ?? throw new InvalidOperationException("Connection string 'DataDb' not found.");

    var services = new ServiceCollection();
    services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));
    services.AddDbContext<CacheDbContext>(o => o.UseSqlServer(connectionString));
    services.AddIdentity<ApplicationUser, IdentityRole>( /* relaxed policy options */ )
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
    services.AddLogging();

    return services.BuildServiceProvider().CreateScope();
}
```

> For Azure SQL serverless targets, consider adding `ConnectTimeout = 120` via `SqlConnectionStringBuilder` and `.EnableRetryOnFailure(5, TimeSpan.FromSeconds(15), null)` on the EF options — see `CSP101StepDefinitions` for the pattern.

### Dynamic test users

Some scenarios (e.g. account deactivation, account registration) need a fresh user that does not exist at seed time. The pattern is:

1. Create the user with a unique `Guid`-based email in a `Given` step.
2. Clean it up in a `[AfterScenario("your-tag")]` method using `UserManager.DeleteAsync`.
3. Use a dynamic email like `$"testfeature{Guid.NewGuid().ToString("N")[..8]}@test.com"` to avoid collisions between runs.

---

## Email Test Patterns

`TestWebAppHost` always forces `EnableEmailTestEndpoint = true` via in-memory config override. This exposes two test-only endpoints that return token URLs as plain text, allowing scenarios to bypass a real email inbox.

### Password reset

```csharp
// Get a fresh reset URL for a user (does not require them to submit the form first)
var link = _passwordResetDriver.GetPasswordResetLink("alex@test.com");
// Navigate to the reset form
_passwordResetDriver.NavigateToResetLink(link);
// Or navigate + fill + submit in one call
_passwordResetDriver.NavigateToResetLinkAndSubmit(link, "NewPass1!", "NewPass1!");
```

### Email confirmation / registration

After `CSP-134`, registration redirects to `/Account/RegisterConfirmation` instead of auto-signing in. Scenarios that need a verified user must:

```csharp
// Register the new user (navigates through the UI)
_emailVerificationDriver.RegisterNewUser(email, password);

// Get a fresh confirmation URL for the new user
var link = _emailVerificationDriver.GetEmailConfirmationLink(email);

// Navigate to it (simulates clicking the email link)
_emailVerificationDriver.NavigateToVerificationLink(link);

// Now the user can log in
_authDriver.PreformLoginForUser(email, password);
```

Seeded personas (`alex@test.com`, `patricia@test.com`, `lily@test.com`) already have `EmailConfirmed = true` — no verification step is needed when logging in as them.

---

## Page Element IDs

| Page | Element ID | Purpose |
|---|---|---|
| Any page | `userDropdownNavDisplay` | Detect logged-in user (nav bar) |
| Any page | `logoutBtn` | Logout button |
| `/Account/Login` | `emailField` | Email input (**not** `Email`) |
| `/Account/Login` | `passwordField` | Password input |
| `/Account/Login` | `RememberMe` | Remember me checkbox |
| `/Account/Login` | `submitBtn` | Login submit button |
| `/Account/Login` | `loginForm` | The `<form>` element |
| `/Account/Login` | `passwordResetSuccessMessage` | Success banner after completed password reset |
| `/Account/Login` | `emailNotVerifiedMessage` | Warning banner when unverified user tries to log in |
| `/Account/Login` | `resendVerificationBtn` | "Resend Verification Email" button in the warning banner |
| `/Account/ForgotPassword` | `forgotPasswordEmail` | Email input |
| `/Account/ForgotPassword` | `sendResetEmailBtn` | Submit button |
| `/Account/ForgotPassword` | `resetEmailSentMessage` | "Check your email" success banner (shown regardless of whether email exists) |
| `/Account/ResetPassword` | `newPasswordField` | New password input |
| `/Account/ResetPassword` | `confirmPasswordField` | Confirm password input |
| `/Account/ResetPassword` | `resetPasswordBtn` | Submit button |
| `/Account/ResetPassword` | `resetPasswordError` | Model-level validation summary div (invalid/expired token) |
| `/Account/ResetPasswordInvalid` | `invalidResetLinkMessage` | Error div on the dedicated invalid-link page |
| `/Account/ResetPasswordInvalid` | `requestNewResetLinkBtn` | Link to request a new reset link |
| `/Account/RegisterConfirmation` | `registrationConfirmationMessage` | "Check your email" message after registration |
| `/Account/RegisterConfirmation` | `resendFromConfirmationLink` | Link to resend verification from this page |
| `/Account/VerifyEmail` | `emailVerifiedSuccessMessage` | Success message after a valid confirmation link |
| `/Account/VerifyEmail` | `loginAfterVerificationBtn` | "Log In" button on the success page |
| `/Account/VerifyEmail` | `emailVerificationErrorMessage` | Error when confirmation token is invalid/expired |
| `/Account/VerifyEmail` | `requestNewVerificationBtn` | Link to request a new verification link |
| `/Account/ResendVerification` | `resendVerificationEmail` | Email input on the resend form |
| `/Account/ResendVerification` | `resendVerificationSubmitBtn` | Submit button on the resend form |
| `/Account/ResendVerification` | `resendVerificationSentMessage` | "Check your email" success banner after resend |
| `/Sighting/Create` | `Latitude` | Latitude input |
| `/Sighting/Create` | `Longitude` | Longitude input |
| `/Sighting/Create` | `Timestamp` | Timestamp input |
| `/Sighting/Create` | `Description` | Description textarea |
| `/Sighting/Create` | `UploadedImage` | Image file upload |
| `/Sighting/Create` | `SubmitBtn` | Form submit button |
| `/Sighting/Gallery` | `filterAll` | "All Sightings" toggle button |
| `/Sighting/Gallery` | `filterMine` | "My Sightings" toggle button |
| `/Sighting/Gallery` | `emptyStateMine` | Empty-state div when "My Sightings" has no results |
| `/Sighting/Gallery` | `sightingsGrid` | Container `div` holding all card wrappers |
| `/Sighting/Gallery` | `currentUserId` | Hidden `<span data-user-id="…">` with the logged-in user's ID |
| `/Sighting/Gallery` | `.sighting-card-wrapper[data-user-id]` | Per-card wrapper; `data-user-id` matched by JS against the current user |
| `/Sighting/Gallery` | `.sighting-attribution` | `<span>` inside each card showing the submitter's `UserName` |

**Access-denied detection:** Check if `driver.Url` contains `/account/login` (case-insensitive) — a redirect to the login page is how the app signals that the request required authentication.

---

## Running the Tests

```bash
# Run only the acceptance tests (WSL2: include HeadlessSelenium flag)
AcceptanceTesting__HeadlessSelenium=true \
  dotnet test src/MH.Capstone.Tests.Acceptance/MH.Capstone.Tests.Acceptance.csproj

# Run all tests in the solution
dotnet test src/MH.Capstone.sln

# Watch Chrome during a run (set HeadlessSelenium = false in local override file, then):
dotnet test src/MH.Capstone.Tests.Acceptance/MH.Capstone.Tests.Acceptance.csproj
```

---

## Writing New Scenarios

1. Choose a tag for your feature (e.g. `@my-feature`) and add it to every scenario in the `.feature` file.
2. Create a step definition class scoped to that tag.
3. Inject `IWebDriver`, `WebDriverWait`, `AcceptanceTestSettings`, and any Driver classes the steps need.
4. Use the standard personas (Alex / Patricia / Lily / James) wherever possible — avoid hardcoding new credentials.
5. If a scenario needs a fresh user, create it dynamically and clean it up in `[AfterScenario("my-feature")]`.
6. Add any new Driver to `TestDependencySetup.CreateServices()` as `services.AddTransient<NewDriver>()`.
7. Regenerate the `.feature.cs` code-behind by running `dotnet build` (WSL2 produces LF line endings — commit the `.feature.cs` file alongside the `.feature` file).

### Minimal example

```gherkin
@my-feature
Feature: My Feature

@my-feature
Scenario: Alex can see the widget
    Given Alex is logged in
    When Alex navigates to the widget page
    Then the widget is displayed
```

```csharp
[Binding]
[Scope(Tag = "my-feature")]
[ExcludeFromCodeCoverage]
public class MyFeatureSteps
{
    private readonly IWebDriver _driver;
    private readonly AcceptanceTestSettings _settings;
    private readonly AuthenticationDriver _authDriver;

    private string BaseUrl => _settings.BaseUrl.TrimEnd('/');

    public MyFeatureSteps(IWebDriver driver, AcceptanceTestSettings settings,
        AuthenticationDriver authDriver)
    {
        _driver     = driver;
        _settings   = settings;
        _authDriver = authDriver;
    }

    [Given("Alex is logged in")]
    public void GivenAlexIsLoggedIn() =>
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");

    [When("Alex navigates to the widget page")]
    public void WhenAlexNavigatesToTheWidgetPage()
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/Widget");
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
    }

    [Then("the widget is displayed")]
    public void ThenTheWidgetIsDisplayed() =>
        _driver.FindElement(By.Id("widgetContainer")).Displayed.Should().BeTrue();
}
```

Drivers and page objects should never hardcode URLs or credentials. Always use `settings.BaseUrl` and the persona constants from `AcceptanceTestSeeder`.
