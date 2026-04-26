# Project Management & PBI Guidelines

This document is the reference guide for creating and updating Jira Product Backlog Items (PBIs) on the CWSA Capstone project. All team members and AI assistants should follow these guidelines when writing or reviewing user stories.

---

## INVEST Principles

Every user story must satisfy all six INVEST criteria before being submitted to Jira.

| Principle | Requirement |
|---|---|
| **Independent** | The story must be self-contained with no inherent dependency on another story. |
| **Negotiable** | Until a story enters an active sprint, it can always be rewritten or changed. |
| **Valuable** | The story must deliver clear value to the end user. |
| **Estimable** | The story must be scoped clearly enough that the team can estimate its size. |
| **Small** | The story must be small enough to plan, task, and prioritize with certainty. |
| **Testable** | The story must provide enough detail for test development to be possible. |

---

## Required Jira Fields

Every Jira issue must have the following fields populated before it is considered ready for sprint planning.

### Team
Always assign: **MH Development Team** (unless explicitly directed otherwise).

### Story Point Estimate (SPE)
Use **powers of 2 only**: 1, 2, 4, 8, …

The general rule is that a story should be **no more than 4 points**. If a story feels larger than 4 points, ask yourself whether it can be broken into smaller, independent stories.

| Points | When to use |
|---|---|
| **1** | Minor bug fix; UI-only update; no new testing or back-end code, or only minimal/routine changes to existing tests and back-end. |
| **2** | Larger full-stack bug fix; larger UI-only or back-end-only update or new design; little to moderate testing updates or implementation. |
| **4** | New full-stack feature; heavy back-end work; requires new or large overhauls of all test types (unit, acceptance, etc.). |

> **Note:** Estimates may vary depending on whether existing infrastructure or prior experience is available. Use the table as a guide — the same type of work can reasonably land at a different point value given context.

---

## User Story Structure

Every Jira issue must include the following sections in order.

### 1. Story Case (Issue Summary / Title)

Write in standard user story format:

```
As a <role>, when <context>, I want <goal> so that <benefit>.
```

### 2. Description

- 2–4 sentences of background explaining the current state and what this story changes.
- A bulleted list of specific behavioral requirements the implementation must satisfy.

### 3. Assumptions / Preconditions

Organize into four subsections:

- **Functional Assumptions** — what the system already provides that this story depends on
- **Security Assumptions** — authentication, authorization, and data visibility rules
- **User Experience Assumptions** — UI behavior, empty states, transitions, labeling
- **System Behavior Assumptions** — backend/data layer behavior, performance, pagination

### 4. Acceptance Criteria

Write all criteria as **Gherkin scenarios** using `Given / When / Then`, wrapped in a fenced Gherkin code block:

````
```Gherkin
Scenario: <scenario name>
    Given <precondition>
    When <action>
    Then <expected outcome>
    And <additional assertion>
```
````

Each behavioral requirement in the Description must map to at least one scenario. Cover:
- Happy path
- Alternative paths
- Empty states
- Security / visibility rules

---

## Example User Story

The following is a canonical example of a well-formed story for this project.

---

**Story Case:**
> As a User, when I visit the gallery page, I want to view sightings submitted by all users so that I can explore the broader community's observations, while still being able to filter the gallery to show only my own sightings when I choose.

**Description:**

Currently, the gallery page displays only the authenticated user's own sightings. This story expands the gallery to show sightings from all users by default, turning it into a community-wide feed. Users retain the ability to filter the gallery down to only their own submissions at any time.

Requirements:
- Display all sightings from all users by default, sorted by most recent
- Show relevant attribution on each sighting card (e.g., submitted by username or display name)
- Provide a filter control (toggle or dropdown) allowing the user to switch between "All Sightings" and "My Sightings"
- Persist the selected filter for the duration of the session (or until changed)
- Respect existing visibility/privacy rules — private sightings must not appear in the community view

**Assumptions / Preconditions:**

*Functional:*
- The application already has an authenticated user session with a resolvable user identity
- Sightings have an owner/author relationship stored in the database
- The gallery page is backed by a data-fetching layer that can be extended to support filtered queries
- Sighting cards have sufficient space to display attribution

*Security:*
- Only authenticated users can access the gallery page
- Private or unlisted sightings are excluded from the "All Sightings" view regardless of filter state
- The API endpoint enforces server-side filtering — client-side filter state alone does not bypass visibility rules
- User identity shown in attribution is limited to publicly shareable profile information

*User Experience:*
- The filter control is clearly labeled and discoverable without instruction
- The default state ("All Sightings") is communicated clearly
- Switching filters does not cause a full page reload
- If a user has no sightings, the "My Sightings" view displays an empty state with a prompt to submit their first sighting

*System Behavior:*
- The gallery data query supports an optional authorId filter parameter
- Pagination or infinite scroll continues to work correctly under both filter states
- Performance is acceptable when loading all community sightings

**Acceptance Criteria:**

```Gherkin
Scenario: Default gallery shows all community sightings
    Given an authenticated user navigates to the gallery page
    When the page loads with no filter selected
    Then sightings from all users are displayed
    And each sighting card shows the submitting user's attribution

Scenario: User filters gallery to their own sightings
    Given an authenticated user is on the gallery page
    When the user selects the "My Sightings" filter
    Then only sightings submitted by the authenticated user are displayed
    And the filter control reflects the active "My Sightings" state

Scenario: User clears the filter to return to community view
    Given an authenticated user has the "My Sightings" filter active
    When the user selects the "All Sightings" filter
    Then sightings from all users are displayed again
    And the filter control reflects the active "All Sightings" state

Scenario: Private sightings are excluded from community view
    Given a user has submitted a sighting marked as private
    When any other user views the gallery in "All Sightings" mode
    Then the private sighting is not visible to them

Scenario: Empty state when user has no sightings
    Given an authenticated user has not submitted any sightings
    When the user selects the "My Sightings" filter
    Then an empty state message is displayed
    And the user is prompted to submit their first sighting

Scenario: Filter persists within the session
    Given an authenticated user has selected the "My Sightings" filter
    When the user navigates away and returns to the gallery page within the same session
    Then the "My Sightings" filter remains active
```

---

## AI Agent Attribution

When an AI agent creates or modifies a Jira PBI description, it must append the following note at the very bottom of the description field:

```
---
AI Agent <Agent Name> assisted in the creation and/or modification of this PBI.
```

Replace `<Agent Name>` with the name of the AI agent or model used (e.g., `Claude Sonnet 4.6`).

---

## Pre-Submission Checklist

Before creating or updating a Jira issue, confirm all of the following:

- [ ] Story case follows `As a / when / I want / so that` format
- [ ] All six INVEST criteria are satisfied
- [ ] Description includes background context and a bulleted requirements list
- [ ] Assumptions are organized into the four subsections
- [ ] Every requirement maps to at least one Gherkin scenario
- [ ] Gherkin scenarios cover happy path, alternative paths, empty states, and security rules
- [ ] Story is small enough to be completed within a single sprint
- [ ] Team is set to **MH Development Team**
- [ ] Story point estimate is set (1, 2, or 4) using the SPE guidelines above
- [ ] AI agent attribution note appended to the bottom of the description (if created or modified by an AI agent)
