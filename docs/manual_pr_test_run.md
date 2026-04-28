# Running the CI Pipeline Manually on a PR

The complete test suite does **not** run automatically on open PRs. It only runs when a PR is **approved** — approval triggers the full pipeline (build, test, deploy, merge). Until then, all test runs must be triggered manually using the workflow described below.

## When to use this

- You want to validate your PR against the full CI pipeline **before requesting review**.
- You want to re-run tests after pushing new commits while the PR is still unapproved.
- Your PR is in **draft state** and you need an early pipeline run.

## How to run it

1. Go to [Run Complete Test Suite on PR (Manual)](https://github.com/jmcshane22/MonmouthHoldemCapstone/actions/workflows/manual_pr_test_run.yml) in the Actions tab.
2. Click **"Run workflow"**.
3. Enter the **PR number** you want to test (e.g. `123`).
4. Click the green **"Run workflow"** button to confirm.

The triggered run will appear in the Actions tab under **"Run Complete Test Suite (All Tests)"**, not under the manual dispatch workflow.

## What happens after approval

Once a PR is approved and is up-to-date with its base branch, the pipeline runs automatically:

1. Full test suite runs.
2. App is deployed to the matching Azure environment (staging for `dev`-targeted PRs, production for `main`-targeted PRs).
3. PR is auto-merged into the base branch.
4. A GitHub Release is created.

No manual action is needed after approval.

## Notes

- A short-lived branch is created internally to facilitate the dispatch and is automatically deleted once the run starts. You do not need to do anything with it.
- This workflow requires the `WORKFLOW_DISPATCH_PAT` repository secret to be configured with Actions read/write access. If the dispatch step fails with an authentication error, contact a repo admin.
