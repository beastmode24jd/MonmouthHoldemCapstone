# Sprint Retrospective - Sprint 6

**Team:** Monmouth Holdem Wildlife Aid Dev Team
**Date:** 05/11/2026
**Sprint Duration:** May 12, 2026 - May 26, 2026 (2 Weeks)
**Scrum Master:** Arin

---

## 1. Sprint Summary

### 🎯 Goal Outcomes

- Final wrap-up of new feature development (INVEST), begin cleaning up bugs in preparation for AES.
- Write down any new bugs for Sprint 7 development.
- Polish current major features (Anidex, Sightings, Badges, Offline Queue)

### 📊 Metrics

- Planned Story Points: 38
- Completed Story Points: 34
- Carryover Story Points:  4
- Bugs Found During Sprint: 2 (Offline Sightings Queue Unaccessible Offline: CSP-207, 
    Clubs showing Email instead of Display Name: CSP-201)
- Production Incidents (if any): 0

### 🚀 Major Deliverables

- Progression display and incrment system, for Badges that take a threshold of actions to hit.
- A details page for each Sighting, available when the user clicks on the card in the Sightings Gallery.
- A queueing service for offline Sighting uploads, so a lack of an internet connection does not disrupt use of the app.
- Ability to follow users.
- Bug fixes of pre-existing features (Email Service Queue, Lat-Long Sighting field autopopulation,
    Real-time Leaderboard updates, Anidex Species Scoring Refinement).

---

## 2. What Went Well ✅

- Sprint 6 was quickly planned, developers got their assignments earlier.
- Continued consistent scrum communication, meetings, and updates between developers.
- Majority of major features and code polish were submitted and merged before the deadline.

---

## 3. What Didn't Go Well ❌

- Due to Badge UX Refinement, the Admin Report story had to be carried over to Sprint 7.
- Continued fragile Acceptance testing, but we have been working to improve Acceptance test reliability.
- Some PBIs were not completed this sprint due to a combination of poor PBI grooming & lack of adequate time-allotment / time-management for completing that PBI.

---

## 4. What Did We Learn 📚

- If all Acceptance Gherkin tests start failing, the problem is likely to be a merge conflict or error in the test Setup process.
- Acceptance testing cannot be run in parallel for the CI/CD pipeline due to the use of a single, Azure-hosted database.
- The GitHub Actions for publish and deployment cannot be cancelled once it stops due to status checking in the entire pipeline.
- If the CICD pipeline - specifically the acceptance tests - are run too often, the Azure database can sometimes fail a test on a one-off failure due to a cloud-hosting issue. This may also sometimes happen just becuase Azure gives up.
  - If a single test fails with a message "Microsoft.Data.SqlClient.SqlException (0x80131904): Database 'systemTesting' on server 'wildlifeaid2.database.windows.net' is not currently available. Please retry the connection later. If the problem persists, contact customer support, and provide them the session tracing ID of XXXXX," then rerun the pipeline as that is most likey an Azure failure and not anything related to the test run.

---

## 5. Process Review 🔍

### Planning

- For Sprint 6, we kept in mind that Sprint 7 would involve app polishing and bug fixing.
- Sprint 6 stories were created and assigned quickly.
- For Sprint 7, we want to create seed data for the app demo presentation at AES.

### Development

- Sprint 6 was quickly planned out and assigned, but the bulk of feature development kicked off after the mid-sprint progress meeting.
- Acceptance testing in CI/CD pipeline increased in time per automated run and check, explained in further detail below.
- Fewer explicit merge conflicts between developers compared to previous Sprints.

### Testing

- Sprint 6 used Gherkin, Selenium, and NUnit testing for features: services, front pages, controllers, etc.
- As mentioned, there were issues towards the end of the sprint with conflicting test run results on CI/CD versus local machines.
- CI/CD testing of PRs increased compared to last Sprint, taking 45+ minutes to run through Git actions.

### Reviews & Submission

- JD handled reviewing PRs, approving them for Staging and Production.
- CI/CD Pipeline Actions Bot approved PRs as well when the pipeline passed, indicating successful run.

### Deployment

- CI/CD Acceptance testing conflicts and failures were a common theme for feature PRs being kept from Staging.

---

## 6. Additional Notes

- N/A.