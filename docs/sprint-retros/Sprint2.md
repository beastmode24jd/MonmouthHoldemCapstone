# Sprint Retrospective - Sprint 2

**Team:** Monmouth Holdem Wildlife Aid Dev Team
**Date:** 03/02/2026
**Sprint Duration:** Feb. 17 - March 3, 2026(2 Weeks) 
**Scrum Master:** JD McShane
---

## 1. Sprint Summary

### 🎯 Goal Outcome
- Goal was to build out core featues of the app that make us stand out from others like sightings, mapping, etc.   
    - Secondary goal of getting all developers acustom to the codebase and it's standards.
- This sprint's goal was achived with core implementation of scoring, user badges and species lookup (via api).
    - The secondary goal was also met as all deveopers used the repository pattern and followed proper codebase standards for all feature development. That said, further work can be done to ensure that all developers understand and follow codebase standards before inital sprint development and to avoid catching incorrect implmentations during PR reviews.

### 📊 Metrics
- Planned Story Points: 25
- Completed Story Points: 25
- Carryover/Uncompleted Story Points: 0
- Bugs Found During Sprint: 2
- Production Incidents (if any): 0

### 🚀 Major Deliverables
- CSP-52 Sightings Map
- CSP-97 Leaderboard
- CSP-58 Wildlife Entry Search
- CSP-104 Scoring Algorithm
- CSP-103 User Achivments

---

## 2. What Went Well ✅
- Team has had good coordination during sprint development and in communicating meetings, workflows, and feature completion timelines (PRs)
- Team is working/meshing well with each other (morale).
- Team has had productive and timely remote communication (Discord).

---

## 3. What Didn't Go Well ❌
- Time management and "agile" development flow during the sprint (not doing everything in one weekend).
- Team had unclear understanding of testing requirements (Gherkin).
- Communication/understanding of codebase standards - i.e. using repository pattern.

---

## 4. What Did We Learn 📚
- Team should make sure to communicate early and often during feature development in hopes of further reducing major PR review reworks
- Team should review the Canvas Sprint Assignment to have a clear understanding of that sprint's expectations from the course/professors
- Team lead should create a better and more clear outline document listing codebase expectations and overall development guidelines, resources and examples.

---

## 5. Process Review 🔍

### Planning
- Continue current Agile meeting schedule/timelines.
    - Was brought up that Sprints 4-END will need to have team-specific Agile meetings rescheduled due to developer class conflicts. Deadline for meeting rescheduling is prior to Spring Break start.
    - Note: Chris/TA Weekly Meetings are still ok at 6 p.m. Tuesdays.

### Development
- Ensure team understands codebase standards and patterns.

### Testing
- Remember to use Mocking and only use InMemoryDatabase (EF Core) for Integration and End-to-End (System) testing.

### Reviews & Submission
- Developers should work to get PRs in further from the sprint deadline to help with time management, Team Lead PR review turnaround and allowing for extra wiggle room for possible required PR changes and/or feature failures during review/retrospective.
- A CI pipeline for ensuring a PR builds, passes tests and can successfully migrate EF Core (aka no pending model changes; EF Migrations are runnable).

### Deployment
- Curtrent system with CI/CD is working well.

---

## 6. Additional Notes

- 
- 
- 