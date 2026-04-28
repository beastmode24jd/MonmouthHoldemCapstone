# PR CI/CD Workflow

## Overview

The CI/CD pipeline has two distinct phases:

1. **Pre-review testing** — run the full test suite manually against an unapproved PR to validate it before requesting review.
2. **Approval-triggered deploy and merge** — once a PR is approved and up-to-date with its base branch, the pipeline runs automatically: full test suite → build → deploy → auto-merge → GitHub Release.

**Auto-merge only fires if the deployment succeeds.** A passing test suite alone is not sufficient — the deployment to the target environment must also complete successfully before the PR is merged.

---

## Running Tests Before Approval

There are two ways to trigger the full test suite on an unapproved PR.

### Option 1: `run-ci` label (recommended)

Add the `run-ci` label to your PR. The workflow:

1. Removes the label immediately (so it can be re-applied to trigger another run).
2. Dispatches the full test suite against the PR's current head commit.
3. Posts a comment on the PR with a link to the run and a pending status check.
4. When the run completes, **automatically posts an approving or request-changes review** on the PR based on the result.

This is the preferred method because the bot review counts toward the approval requirement — if tests pass, the PR is one step closer to triggering the deploy pipeline.

### Option 2: Manual workflow dispatch

1. Go to [Run Complete Test Suite on PR (Manual)](https://github.com/jmcshane22/MonmouthHoldemCapstone/actions/workflows/manual_pr_test_run.yml) in the Actions tab.
2. Click **"Run workflow"**.
3. Enter the **PR number** you want to test (e.g. `123`).
4. Click the green **"Run workflow"** button.

This triggers the same full test suite and posts a comment + status check, but does **not** post a review. Use this when you want a test run without automatically affecting review state.

In both cases, the triggered run appears in the Actions tab under **"Run Complete Test Suite (All Tests)"**.

---

## What Triggers the Deploy Pipeline

The deploy and auto-merge pipeline (`PR Deploy and Auto-Merge`) fires on:
- A PR review being submitted
- A push to the PR head (synchronize)
- A PR being marked ready for review

**All of the following conditions must be true for the pipeline to proceed:**

- The PR targets `dev` or `main`
- The PR is **not a draft**
- The PR has at least one **Approved** review
- The PR is **up-to-date with its base branch** — it must not be behind `dev` (or `main`); update the branch before expecting the pipeline to fire
- The PR has **no merge conflicts**

If any condition is not met, the pipeline skips silently. The most common reason for a stuck PR is being behind the base branch — merge or rebase to bring it current.

---

## What Happens After Approval

Once all conditions are met, the pipeline runs in order:

1. **Full test suite** — build, unit tests, EF Core validation, integration tests, acceptance tests.
2. **Publish** — `dotnet publish` + EF Core migration bundles built and uploaded as an artifact.
3. **Deploy to environment** — artifact deployed to Azure App Service; EF Core migrations applied.
   - `dev`-targeted PRs → `azure_staging` environment (prerelease release)
   - `main`-targeted PRs → `azure_prod` environment (non-prerelease release)
4. **Auto-merge** — PR is merged into its base branch. **This step only runs if the deployment succeeds.** If the deploy fails, the PR is not merged.
5. **GitHub Release** — a versioned release is created. Only runs if auto-merge succeeded.

No manual action is needed after approval, provided the PR is up-to-date with its base branch.

---

## Notes

- **Required branch protection status check:** `PR Deploy and Auto-Merge / Deploy to Environment`
- The `run-ci` label is removed automatically as soon as it is applied — this is intentional so it can be re-added to trigger another run.
- Both manual and label-triggered runs create a short-lived `ci/*-trigger-<pr>` branch internally to facilitate the dispatch. These branches are deleted automatically once the run starts.
- The `WORKFLOW_DISPATCH_PAT` repository secret (Actions read/write scope) is required for the dispatch step. If the dispatch fails with an authentication error, contact a repo admin.
- Build versioning format: `YYYY.M.<run_number>.<run_attempt>`
