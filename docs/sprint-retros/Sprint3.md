# Sprint Retrospective - Sprint 3

**Team:** Monmouth Holdem Wildlife Aid Dev Team
**Date:** 03/16/2026
**Sprint Duration:** Mar. 2, 2026 - Mar. 16, 2026 (2 Weeks)
**Scrum Master:** JD McShane

---

## 1. Sprint Summary

### 🎯 Goal Outcome
- Continue feature building of Core Application Content (Sightings, Map display, points multipliers, etc).
    - Secondary goal of fixing bugs and bringing Sprint 1 code up-to-date with our current workflow and testing structure.
- This sprint's primary goal was largely achieved with successful implementations of Map Sighting icons, a User Sighting Gallery, point multipler systems, and adding in an Emailer system.

### 📊 Metrics
- Planned Story Points: 24
- Completed Story Points: 20
- Carryover/Uncompleted Story Points: 4
- Bugs Found During Sprint: 2 (CSP-149, CSP-150)
- Production Incidents (if any): 2

### 🚀 Major Deliverables
-  User's Sighting Gallery
-  Further enhancements to the points system (multipliers, login streaks)
-  Sightings markers on the Map based on Location
-  Bug fixes and general enhancements for the rest of the app
-  Incorporating Emailer Service to app notifications
-  Page reporting system.

---

## 2. What Went Well ✅
- Development timelines were met more effectively than prior Sprints.
- Team members got time to review and test their code on the live staging website.
- Fewer PR merge conflicts between feature development branches.
    - Git commits with rebase and similar edits to test or setup files (Program.cs) were the main conflicts.

---

## 3. What Didn't Go Well ❌
- Mid-sprint review did not have many Sprint 3 features implemented for the meeting.
- There were issues between developers on Migration changes.
    - Migrations seemed to malfunction slightly while multiple developers were changing the schema at once.
- There were issues with duplicates for page report IDs and DbContext schemas, causing visual and/or backend connection errors.
- Algorithm-based features were difficult to fully showcase during the live Sprint demo.

---

## 4. What Did We Learn 📚
- Migrations are powerful, yet fragile.
- Merge conflicts are not ideal.
- Working with cookies can get complicated quickly.
- Remember to filter entities on the database, not the web server.
    - "Get all async strikes again."
- Deploy earlier, and ensure deployment scripts are correct.
- Prepare scripts and/or ways to showcase algorithmic features beforehand.

---

## 5. Process Review 🔍

### Planning
- Sprint planning can remain as-is, works well.
- Tuesday meeting schedule can remain as-is, continue to work on building Jira backlog.

### Development
- Current Development process is working great.

### Testing
- Continue to work on adding a full range of tests to our application.

### Reviews & Submission
- Remote testing for codebase security is working well, and as expected.
- Code checks are working well for catching potentially disastrous issues.

### Deployment
- Current automated process works great. Team lead does not have to consistently manually interfere.
- Page reporting ran into duplicate key errors during Sprint demonstration.
- Caching the Animal Lookup API call ran into errors with duplicate DbContext files during deployment, and/or the Sprint demonstration.

---

## 6. Bugs & Issues

### Bug: UTC DateTime Display
- **Description:** (CSP-131) The database naturally saved Badges and Sightings in UTC time, using a DateTime object type. This meant that the front-end display would not accurately update or show the "BadgeEarned" or "Timestamp" properties of Badges and Sightings in the timezone of the user's device.
- **Severity:** Low
- **Status:** Resolved (In-Sprint)
- **Resolution:** Ran EF migration (and database) updates to update DateTime values to DateTimeOffset. Added global JS script to save the user device's timezone to a local cookie (as an IANA timezone ID). This is converted by the Dashboard, Account, and Sighting Controllers, to accurately capture and display the local times Badges and Sightings were earned by the user.

### Bug: Remember Me
- **Description:** (CSP-126) When toggling the "Remember Me" checkbox on the Login page, Program.cs had references to two separate cookies for Remember Me attribute storage. These conflicted, and confused the program on which cookie to reference/call. The website would proceed to *not* remember the user.
- **Severity:** Low
- **Status:** Resolved (In-Sprint)
- **Resolution:** Removed the duplicate cookie authentication setup from Program.cs.

### Bug: Admin Deactivation
- **Description:** (CSP-100) When going to deactivate a user, there would be a conflict if the Admin account did not have the same password as the user. This would result in the AuthenticationService call for deactivation not successfully going through, as it would need the user's password for account checking.
- **Severity:** Low
- **Status:** Resolved (In-Sprint)
- **Resolution:** Rewrote the AuthenticationService method call to remove the password string, and instead implemented account checking into the logic of the regular and Admin Management Deactivation pages.

### Bug: Page Report Error 
- **Description:** (CSP-150) When going to report a page on Staging, the JS would save the initial report on Production, then encounter issues with needing a unique ID (but using the same one) for the second report on Staging. This would cause a 500 Network error to be displayed on Staging, when the user report was saved.
- **Severity:** Moderate
- **Status:** WIP Resolution
- **Resolution:** Updating of JS fetch try/catch to display specific conflict error message when backend alerts to a record conflct.

### Bug: Caching Results of Animal Lookup API Call
- **Description:** (CSP-149) Due to a secondary DBContext, the context was missed during deployment, causing the remote servers to lose the table schema.
- **Severity:** High
- **Status:** WIP Resolution
- **Resolution:** Update deployment scripts to include the secondary `CacheDbContext` in current and future deployments; investigate ways to catch missed context deployments for future sprints.

---

## 7. Additional Notes
- Need to plan Sprint 4, for Spring Term.
    - Planned for Monday, March 30, 4:30 - 6:00 p.m.
- Team can and will retain Tuesday meetings with Chris from 6:00 ~ 6:50 p.m.
- Cannot do Monday 12:00 meetings anymore (Sprint deployment review and retrospective)
    - Sprint deployment review and retrospective meetings will shift to being bi-weekly on Mondays, 4:30 ~ 6:00 p.m.
- Sprint planning will continue to be bi-weekly on Tuesdays, 4:00 ~ 5:00 p.m.
- Third weely Scrum meetings will remain to be Fridays, 10:00 ~ 10:30 a.m.
- As-needed group development time will move to on Fridays, 10:30 ~ 1:00 p.m. Originally was Mondays & Wednesdays from 12-2.