# Project Architectural Guidelines

## 1. Folder Structure and Naming Conventions
### 1.1 Projects & Folder Structure
#### <u>1.1.1 TestSuite (Solution Folder)</u>
- **MH.Capstone.Domain.Tests.Unit**
    - DataAccess
    - Services
    - TestInternal
- **MH.Capstone.Tests.Acceptance**
- **MH.Capstone.Tests.Integration**
- **MH.Capstone.WebApp.Tests.Unit**

#### <u>1.1.2 Root Level</u>
- **MH.Capstone.WebApp**
    - wwwroot
        - css
        - js
        - lib
            - bootstrap
            - jquery^
            - jquery-validation^
            - jquery-validation-unobtrusive^
    - Controllers
        - Api
    - Models
    - Views
        - Shared
        - *Folders matching controller*
- **MH.Capstone.Domain**
    - DataAccess
        - Abstraction
    - DataModels
    - Dtos
    - Services
        - Abstraction

*^JQuery used only for Bootstrap; will not be used by developers.*

## 2. DOT NET Core Version
Our team will use .NET 9, specifically SDK version 9.0.309

## 3. Front End CSS Library
Our team will use [Bootstrap 5.3](https://getbootstrap.com/docs/5.3/getting-started/introduction/)

## 4. JavaScript Framework and Front-End JS Libraries
### 4.1 JavaScript Plan
Our team will use "plain old Javascript" within the frontend of our project.

### 4.2 Front-End JS Libraries
Planned front-end JS libraries as of Feb. 2, 2026
- [Jest](https://jestjs.io/docs/getting-started)
    - For JavaScript Unit Testing
- [Google's Maps JavaScript API - Client-Side](https://developers.google.com/maps/documentation/javascript/overview)
    - For map related plotting, location selecting and location-oriented information displays

## 5. Git Branching & General Development Workflow
### 5.1 Feature Branches
<u>Feature branches will be named after the ticket/issue number in Jira the branch is being created to complete development for</u> (i.e. "CSP-13" for Jira Ticket ID CSP-13). 

This naming convention/workflow creates consistency and clarity in what exactly that branch is for and the goal of that feature branch.

### 5.2 Merging & Post-Feature Development Checklist
Before creating a GitHub Pull Request as [outlined in section 5.3](#53-github-pull-request-workflow--procedures), first ensure you have completed the following checklist to ensure compatibility of code and a successful PR.

- [ ] All Relevant Tests have been written and are passing
    - [ ] Acceptance Tests in BDD style
    - [ ] Unit Tests, including Jest for any JS Code
    - [ ] Integration Tests, when/where applicable
- [ ] Review Jira Item to ensure you have completed the task
- [ ] <u>Checkout your dev branch</u> and run the following commands to ensure you are current
    - [ ] `git pull origin dev`
    - [ ] `git pull upstream dev`
- [ ] Once current with your origin fork and the main upstream repo, merge your feature branch into the dev branch. <u>Please avoid merge commits here where possible.</u>

### 5.3 GitHub Pull Request Workflow & Procedures
Once you have followed the steps in [section 5.2](#52-merging--post-feature-development-checklist), create a Pull Request (PR) in the [main repo](https://github.com/jmcshane22/MonmouthHoldemCapstone). Follow the below steps to ensure a clean and proper PR:
- [ ] Base branch should be jmcshane22:dev
- [ ] Target branch should be your local/fork dev branch.
- [ ] PR title should be "[JIRA-ID] Jira Item title".
- [ ] PR body/description should follow and match [this template](./pr-templates/pull-request-body-template.md).
    - See the [PR Body Example file](./pr-templates/pr-body-example.md) for a detailed, acceptable example PR.
- [ ] Attach Repo Manager JD McShane (jmcshane22) and Copilot as reviewers
- [ ] Attach yourself and any other code authors as assignees to the PR
- [ ] Attach all approprate labels to the PR
- [ ] Attach the current sprint's milestone as the PR's milestone.

## 6. Database Scripting & Naming Conventions
- Database tables (entities) should be 
- Column names should be lowercase and 

## 7. Entity Framework (EF) Core Related-Entity Loading Policy
Our team will use ["Lazy Loading"](https://learn.microsoft.com/en-us/ef/core/querying/related-data/lazy) for [EF Core](https://learn.microsoft.com/en-us/ef/core/) related-entity loading.