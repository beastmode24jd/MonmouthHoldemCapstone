# Sprint Retrospective - Sprint 5

**Team:** Monmouth Holdem Wildlife Aid Dev Team
**Date:** 04/30/2026
**Sprint Duration:** Apr. 14, 2026 - Apr. 28, 2026 (2 Weeks)
**Scrum Master:** Arin

---

## 1. Sprint Summary

### 🎯 Goal Outcomes
- Implement social media features of the app, starting with the "Clubs" feature and dedicated user spaces.
    - Incorporated previously untouched Jira Epic of social media and networking into Wildlife AID.
    - Incorporated user search system, so users can look each other up.
- Worked on Sighting image identification and recognition, incorporated AI scanning of animal and/or species.
- Fix security concern of using the account email as a display name, providing a traditional username instead.
- Refine CI/CD for the project, to improve testing before allowing PRs to reach Staging and/or Production.
- Built out the Anidex, to connect Sightings to the rarity scoring system.
- Streamlined the notification system, by:
    - Creating "Read/Delete all" toggles for the user
    - Creating notification types
        - Creating toggles for those types, for the user's emailer service
- Removed cluttered Dashboard on Login, per Alpha testing user feedback.
    - Moved user profile customization to a dedicated Account Settings page.

### 📊 Metrics
- Planned Story Points: 38
- Completed Story Points: 38
- Carryover Story Points:  0
- Bugs Found During Sprint: 3 (CSP-189: Photo Quality Checker Saving Error, CSP-173: Emailer Service overloaded and crashed despite Email Queue, and CSP-199: Sighting cards on front Club page not displaying image dimensions universally.)
- Production Incidents (if any):  Minor bug with the Photo Quality Checker, of the service awarding points and scoring rarity despite cases where the submitted photo failed the check (CSP-189).

### 🚀 Major Deliverables
- Working Clubs feature, with front page, public and private settings, user invites, and forum chatrooms.
- AI-delivered image identification and description features.
- Anidex lookup for user guidance and goals.
- Refined user account organization and notification options.
- New "display name" for users, to sanitize personal emails from front-end display.

---

## 2. What Went Well ✅

- All team members delivered their major Features before the Tuesday deadline, and had their first stories merged into dev and Staging earlier compared to previous Sprints.
- Major functionality was added to several story Epics at once (Sightings and Anidex, Social Media, User Account services).
- All developers had backend work and feature testing to show, during the mid-sprint review with Chris.
- INVEST stories were prioritized, pushing forward momentum of application development.

---

## 3. What Didn't Go Well ❌

- Conflicting Gherkin tests and conditions caused tests to pass on CI/CD, but fail on local devices.
- During Alpha testing, the Emailer service overloaded and crashed, barring new users from being added.
- Due to failed merges with upstream dev, several PRs had to manually delete automated Gherkin testing files (feature.cs files) from the branch before the PRs would go through.
- Github workflow attempted to add 20+ automated PRs to the repository, which had to be cancelled.

---

## 4. What Did We Learn 📚

- Gherkin/Selenium is fickle, and can change depending on timing, environment, and newly added features.
- AI cannot always fix a testing bug, and merging different features can cause tests to take longer or fail when rerunning on a local machine.
- Github free actions has a price limit, and it is possible to exceed it.
- Updating .gitignore contents does not update .gitignore at the same time for everyone, so manually deleting automated or conflicting files from a PR's commits is important.
- Larger features can be broken down into smaller pieces over multiple Sprints.

---

## 5. Process Review 🔍

### Planning
- Sprint 6 was planned, with Stories assigned to team members before the meeting with Chris.
- Sprint 5 stories were assigned to developers on the first Thursday of the Sprint (16th), with plans to have tangible progress and feature work done before the Tuesday review with Chris.
- Developers were encouraged to get in their first features before the second weekend of Sprint 5, to better combine and merge features onto Staging.

### Development
- Sprint 5 was demonstrated live from the Staging Wildlife AID website.
- Sprint 5 was the first sprint where active AI development was encouraged, for comparison to Sprint 4 momentum.
- Feature progress across developers was more consistent during Sprint 5, compared to previous Sprints.
- Minor inconsistencies across the app, incorporating the new "Display Name" feature over the previous "Email" usernames.

### Testing
- Sprint 5 used Gherkin, Selenium, and NUnit testing for features: services, front pages, controllers, etc.
- As mentioned, there were issues towards the end of the sprint with conflicting test run results on CI/CD versus local machines.
- CI/CD testing of PRs increased compared to last Sprint, taking 10 to 22+ minutes to run through Git actions.

### Reviews & Submission
- JD handled reviewing PRs, and approving them for Staging and Production.
- JD also incorporated Github actions for CI/CD testing towards the end of Sprint 5, automating more of the PR review process.

### Deployment
- CI/CD testing saw an increase in time taken for a PR to pass testing, before it was considered for manual review.
- Manual review was quicker due to fewer last-minute PR completions and merge conflicts.

---

## 6. Additional Notes
- Lots of major work was done for core features of the app.