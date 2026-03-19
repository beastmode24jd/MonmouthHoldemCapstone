# Sprint Retrospective - Sprint 1

**Team:** Monmouth Holdem Wildlife Aid Dev Team
**Date:** 02/16/2026
**Sprint Duration:** Feb. 4 - Feb. 16, 2026 (2 Weeks)
**Scrum Master:** JD McShane

---

## 1. Sprint Summary

### 🎯 Goal Outcome
- Primary goal was to establish core user account functionality and foundational features for the Wildlife AID application, enabling users to create accounts, authenticate, and manage their profiles.
    - Secondary goal of onboarding all developers to the codebase, establishing development workflows, and setting up the production deployment pipeline.
- This sprint's primary goal was largely achieved with successful implementation of user authentication, account management, password recovery, and informational pages.
    - The secondary goal was also met as all developers became familiar with the ASP.NET Core MVC architecture, Entity Framework patterns, and Git branching workflows. Azure deployment infrastructure was successfully established, though it required more effort than anticipated.

### 📊 Metrics
- Planned Story Points: 26
- Completed Story Points: 24
- Carryover/Uncompleted Story Points: 2
- Bugs Found During Sprint: 1
- Production Incidents (if any): 0

### 🚀 Major Deliverables
-  User Authentication / Login
-  User Registration
-  Forgot Password / Password Reset
-  Account Deactivation
-  About Us Page

---

## 2. What Went Well ✅
- Overall development went smoothly with all planned core features being functional and meeting acceptance criteria.
- Team dynamic and collaboration was strong throughout the sprint; developers supported each other through challenges and blockers.
- All core authentication features worked as expected upon completion, with proper security measures in place (password hashing, validation, etc.).
- Team communication via Discord was effective and responsive, with quick turnaround on questions and code reviews.
- Successfully established the repository pattern and service layer architecture that will serve as the foundation for future development.
- First deployment to Azure was successful, giving the team a live product to demo and iterate on.

---

## 3. What Didn't Go Well ❌
- Setting up Azure and live deployment infrastructure was significantly more challenging and time consuming than anticipated.
    - Configuration of App Service, SQL Database, and connection strings required extensive troubleshooting.
    - CI/CD pipeline setup with GitHub Actions had several failed runs before working correctly.
- Some features came in close to the sprint deadline, leaving limited time for thorough PR reviews.
- Initial unfamiliarity with ASP.NET Core Identity framework led to some early implementation challenges.
- Documentation of architectural decisions and codebase standards was lacking at the start of the sprint.

---

## 4. What Did We Learn 📚
- Team needs to be more proactive about getting PRs and features submitted well ahead of deadlines to allow adequate review time.
- Better organization and time management throughout the sprint will help reduce end-of-sprint crunch and improve code quality.
- Early setup of deployment infrastructure is critical - should be one of the first tasks tackled in any new project.
- Clear documentation of codebase standards and patterns should be established before feature development begins.
- Regular check-ins mid-sprint help identify blockers early and keep everyone aligned on progress.

---

## 5. Process Review 🔍

### Planning
- Initial sprint planning was effective in scoping appropriate work for the team's first sprint together.
- Story point estimation was reasonable, though some tasks took longer than expected due to learning curves.
- Backlog grooming sessions helped clarify acceptance criteria and reduce ambiguity.

### Development
- Development workflow was smooth once Git branching strategy and PR process were established.
- Team successfully adopted the repository pattern, service layer architecture, and dependency injection patterns.
- Code reviews were constructive and helped maintain consistent code quality across the team.
- Pair programming sessions were helpful for onboarding and knowledge sharing.

### Testing
- Unit tests were written for core authentication service functionality.
- Manual testing was performed for all features prior to PR submission.
- Test coverage could be improved in future sprints, particularly for edge cases.

### Reviews & Submission
- PR review process worked well but would benefit from earlier submissions to allow more review time.
- Established PR template and review checklist for consistency.
- Team Lead (JD) provided thorough and constructive feedback on all PRs.

### Deployment
- Azure App Service and SQL Database deployment setup was completed successfully.
- CI/CD pipeline was established using GitHub Actions for automated builds and deployments.
- Environment configuration (Development, Staging, Production) was established.
- Entity Framework migrations were integrated into the deployment pipeline.

---

## 6. Bugs & Issues

### Bug: Account Deactivation - Duplicate Username Constraint
- **Description:** When multiple users deactivated their accounts, the system attempted to set all usernames to "Deactivated User", violating the unique constraint on the UserName column.
- **Severity:** Medium
- **Status:** Resolved
- **Resolution:** Modified the deactivation logic to preserve unique usernames while setting the IsDeactivated flag. Display name anonymization is now handled at the presentation layer rather than modifying the database username.

---

## 7. Additional Notes

- First sprint for the team - successfully established workflows, processes, and team norms.
- Foundation laid for future feature development with clean architecture and established patterns.
- Team is well-positioned for Sprint 2 with deployment infrastructure in place and all developers comfortable with the codebase.
- Velocity of 24 story points provides a baseline for future sprint planning.
- Team morale is high and everyone is excited to continue building out the Wildlife AID application.
