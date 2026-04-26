# MH.Capstone.Tests.JavaScript

Jest unit tests for the JavaScript files in `MH.Capstone.WebApp/wwwroot/js/`.

This is a standalone Node/Jest project — it is not a `.csproj` and is not run by `dotnet test`. It has its own dependency graph (`package.json`) and runner (`npm test`).

---

## Prerequisites

- [Node.js](https://nodejs.org/) 18 LTS or later
- npm (bundled with Node)

### Install on Ubuntu/Debian

```bash
curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
sudo apt-get install -y nodejs
```

### Install on Windows

Download from https://nodejs.org/ or use `winget install OpenJS.NodeJS.LTS`.

---

## Setup

Run once from this directory to install Jest and its dependencies:

```bash
cd src/MH.Capstone.Tests.JavaScript
npm install
```

`node_modules/` is gitignored. The `package-lock.json` is committed — always use `npm ci` in CI environments for deterministic installs.

---

## Running tests

All commands should be run from `src/MH.Capstone.Tests.JavaScript/`.

| Command | What it does |
|---|---|
| `npm test` | Run all tests once |
| `npm run test:watch` | Re-run on every file save (development) |
| `npm run test:coverage` | Run all tests and print a coverage report |

### Run a single file

```bash
npx jest __tests__/notifications.test.js
```

### Run tests matching a name pattern

```bash
npx jest -t "isTableEmpty"
```

---

## Project structure

```
MH.Capstone.Tests.JavaScript/
  package.json          # project metadata and npm scripts
  jest.config.js        # Jest configuration (environment, coverage, thresholds)
  README.md             # this file
  __tests__/
    notifications.test.js   # tests for wwwroot/js/notifications.js
```

Source files live in `../MH.Capstone.WebApp/wwwroot/js/`. Tests reference them by relative path:

```js
const { ... } = require('../../MH.Capstone.WebApp/wwwroot/js/notifications');
```

---

## Writing testable JavaScript

For a JS file to be testable with Jest it must:

1. **Export named functions** via a Node-compatible guard at the bottom of the file:

    ```js
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = { myFunc, anotherFunc, initPage };
    }
    ```

2. **Guard browser-only entry points** so they don't execute on `require`:

    ```js
    if (typeof document !== 'undefined') {
        document.addEventListener('DOMContentLoaded', () => initPage());
    }
    ```

3. **Inject side-effecting dependencies** (`fetch`, `location.reload`, global callbacks) as parameters with sensible browser defaults:

    ```js
    function initPage({ fetchFn, reloadFn } = {}) {
        const _fetch  = fetchFn  || fetch;
        const _reload = reloadFn || (() => location.reload());
        // ...
    }
    ```

    Tests can then pass mock implementations without patching globals.

---

## Writing tests

### File naming

Test files live in `__tests__/` and are named `<source-file>.test.js`:

| Source | Test file |
|---|---|
| `wwwroot/js/notifications.js` | `__tests__/notifications.test.js` |
| `wwwroot/js/site.js` | `__tests__/site.test.js` |

### Structure

Group tests with `describe` blocks that mirror exported function names. Keep each `test` focused on a single assertion or behaviour:

```js
describe('isTableEmpty', () => {
    test('returns true when no table exists', () => { ... });
    test('returns false when tbody has rows',  () => { ... });
});
```

### DOM setup

Use `document.body.innerHTML = ''` in `beforeEach` to reset DOM state between tests. Set up only the markup each test actually needs — don't share a large global fixture.

```js
beforeEach(() => {
    document.body.innerHTML = '';
});
```

### Mocking fetch

Pass a `jest.fn()` as `fetchFn` instead of patching the global:

```js
const fetchFn = jest.fn().mockResolvedValue({ ok: true });
await submitMarkAllRead(form, fetchFn);
expect(fetchFn).toHaveBeenCalledWith('/notifications/mark-all-read', expect.objectContaining({ method: 'PUT' }));
```

### Fake timers (setTimeout / setInterval)

Use Jest's fake timers for any code that defers work:

```js
beforeEach(() => { jest.useFakeTimers(); });
afterEach(() => { jest.useRealTimers(); });

test('removes row after 240 ms', () => {
    fadeAndRemoveRow(row, jest.fn());
    jest.advanceTimersByTime(240);
    expect(document.querySelector('tr')).toBeNull();
});
```

---

## Coverage

Running `npm run test:coverage` produces:

- A text summary in the terminal
- An HTML report in `coverage/lcov-report/index.html`
- An LCOV file at `coverage/lcov.info` (for CI coverage tools)

`coverage/` is gitignored. Thresholds are enforced in `jest.config.js` — the build fails if coverage drops below the configured minimums. Raise the thresholds as the test suite grows.

---

## CI integration

Add this step to `.github/workflows/build_test_ci.yml` after the `dotnet test` step:

```yaml
- name: Install Node
  uses: actions/setup-node@v4
  with:
    node-version: '20'

- name: Install JS test dependencies
  working-directory: src/MH.Capstone.Tests.JavaScript
  run: npm ci

- name: Run Jest tests
  working-directory: src/MH.Capstone.Tests.JavaScript
  run: npm test
```
