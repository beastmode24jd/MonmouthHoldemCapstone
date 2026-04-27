# Running the CI Pipeline Manually on a Draft PR

The complete test suite runs automatically on any PR that is **ready for review** (not a draft). If your PR is still in draft and you need the pipeline to run, use the manual dispatch workflow described below.

## When to use this

- Your PR is in **draft state** and you want to validate it against the full CI pipeline before marking it ready for review.
- If your PR is already marked **ready for review**, the pipeline will trigger automatically — you do not need this workflow.

## How to run it

1. Go to the **Actions** tab of the repository.
2. In the left sidebar, select **"Run Complete Test Suite on PR (Manual)"**.
3. Click **"Run workflow"**.
4. Enter the **PR number** you want to test (e.g. `123`).
5. Click the green **"Run workflow"** button to confirm.

The workflow will run the full test suite — build, unit tests, EF Core validation, integration tests, and acceptance tests — against the exact head commit of the PR, including PRs from forked repositories.

## Notes

- A short-lived branch is created internally to facilitate the dispatch and is automatically deleted once the run starts. You do not need to do anything with it.
- The triggered run will appear in the Actions tab under **"Run Complete Test Suite (All Tests)"**, not under the manual dispatch workflow.
- This workflow requires a repository secret (`WORKFLOW_DISPATCH_PAT`) to be configured. If the dispatch step fails with an authentication error, contact a repo admin.
