Project Inception: Wildlife Scavenger App
=====================================

## Summary of Our Approach to Software Development
We are following an Agile methodology with Scrum framework. Our workflow involves weekly sprints and a Git flow where all feature development
will occur on branches derived from "dev" and is merged via approved PR's. We will use Jira for managing user stories and tracking our progression, and Miro for 
group mind mapping and architectural brainstorming. 

## Initial Vision Discussion with Stakeholders
### Product Vision Statement
Monmouth Hold’em’s Competitive Wildlife Scavenger App (CWSA) is a next-generation platform that brings competition, discovery, and education together in the natural world. By blending engaging gameplay with real-world exploration, CWSA redefines how people experience, understand, and connect with nature. From rare and exotic wildlife to the plants in your own backyard, CWSA transforms zoology, botany, and outdoor exploration into an interactive, rewarding experience for everyone.

### Project Details Overview
The app features three main pillars: 
* An **Anidex catalog** to track finds and provide information on different species. 
* A **GPS map** for navigation and locating nature; for mapping found nature.
* **AI photo tools** to verify that a user actually found what they uploaded; give more details to the user. 

Our goal is to make learning about nature fun and active by implementing a point and leaderboard system. We want to make being outside more of a game where you can earn points, level up, get rewards, and climb the leaderboard while learning about the environment.

While competitors like **Pokemon GO** are popular, they rely on fictional monsters, and apps like **iNaturalist** are great for scientific purposes but lack the competitive element. We are combining the best of both worlds by adding missions, clubs, and actual competition to real-world nature spotting.


### Description of Clients/Users
**Nature enthusiasts**: People that enjoy hiking and wildlife photography, or are looking to find outdoor activities
**Competetive Gamers**: People who enjoy competition based activities, ranking systems, leaderboards, achievements, similar to Pokemon GO
**Educational purposes**: Professors, scouts, clubs, schools looking for a way to identify local flora and fauna. An application for structured nature scavenger hunts

### List of Stakeholders and their Positions (if applicable)
Who are they? Why are they a stakeholder?
* Project Maintainer (JD): Will be responsible for code reviews, repository healthy, and making sure deadlines are met
* Dev Team (Arin, Pedro, Marquis): Will be reponsible for implementing features, AI integrationm and mapping logic.
* Coach, Product Owner Technical Advisor, Software Owner (Chris): Will act as the primary stakeholder for project requirements and industry standards.
* Arbiter of graduation (Dr. Morgan): Will be responsible for passing or failing us

## Initial Requirements Elaboration and Elicitation
See Requirements_template for more

### Elicitation Questions
* Gameplay and Scoring Mechanics
    * What specific metrics (rarity, difficulty of terrain, or distance) will define the point values in the proximity scorer?
    * How will the leaderboard handle ties between users who have found the same number of species?
    * What are the daily or weekly limits on "Missions" to prevent users from progressing too quickly?

* Technical and AI Integration

    * How will the application handle species verification if the OpenAI Vision API returns multiple potential matches?
    * What fallback mechanism is needed for users who are in "dead zones" without cellular data for GPS or image uploads?
    * How will the Anidex catalog be updated when new flora or fauna are discovered that aren't in the current database?

* Safety and Community
    * What measures (geofencing) are needed to ensure user safety and prevent trespassing on private property or dangerous areas?
    * How will "Clubs" be moderated to ensure a positive and educational social environment?
    * Will there be a "Junior" mode with restricted location tracking for younger students using the app for educational purposes?

### Elicitation Interviews
    

### Other Elicitation Activities?
    

## List of Needs and Features
* Need: A reliable navigation source to wildlife spots. 
    * Feature: MAP DISPLAY - real time navigation
* Need: An accurate and automatic way to identify species in the wild. 
    * Feature: PHOTO IDENTIFICATION - Instant photo verification
* Need: Create a fair competition system based on discovery difficulty and rarity. 
    * Feature: Proximity Scorer - Calculate points based on distance from said target and rarity multiplier
* Need: A way to track and review personal and potential discoveries. 
    * Feature: ANIDEX- flora/fauna catalog, encyclopedia
* Need: A way for users to engage socically/ group competition. 
    * Feature: LEADERBOARD/CLUBS - Ranking system to provide competition among individuals and clubs
* Need: Structured gameplay loop to keep active users. 
    * Feature: MISSIONS AND CHALLENGES - Objective based tasks for users to complete
* Need: A way to save users information, progress, Anidex. 
    * Feature: USER ACCOUNT/PROFILE - A system personalize settings and record history
## Initial Modeling

[Miro Mindmap](https://miro.com/welcomeonboard/ZTZhVGxWSDhjMWxPbEovdDZacVRsVUdZeURaYk5hVnA5SGpkNG1XajRRTXNCdW5yeXlLblVONlN3Zk9NWUUxdmhLV2g2MXN1bFFEZEMwZjhXYjBaSGh5TlJJU1BheG9ZY05LNDRLaTcxeXZUWGJRVlZNVkxodk1RQ0R6Tlh6Y2hBd044SHFHaVlWYWk0d3NxeHNmeG9BPT0hdjE=?share_link_id=509122394278)

### Use Case Diagrams
Diagrams

### Sequence Diagrams

### Other Modeling
Diagrams, UI wireframes, page flows, ...

## Identify Non-Functional Requirements
* Usability: Mobile application. The user interface will be built with Bootstrap, needs to be responsive and needs to be readable and usable for users as they will be outdoors
* Scalability: C# proximity scorer and leaderboard system should be optimized to handle growing numbers of users 
* Security: All user credentials and Anidex logs must be stored in the SQL Server thats encrypted to protect player privacy
* Performace: The OpenAI Vision API must return identification results within 5 seconds to make sure the gameplay loop remains engaging
* Availability: Google Maps nav and GPS hotspots must be accessible 99% of the time, so long as the user has connection to wifi or cellular data

## Identify Functional Requirements (In User Story Format)

E: Epic  
U: User Story  
T: Task  

7. [E] User Profiles
1. [U] As a new player, I want to create an account so my findings and points are saved to my profile
        a. [T] Need to build a registraion page using ASP.NET or a custom Razor form
        b. [T] Create a SQL table to store user information and profile information (level, points, Anidex, Clubs, Achievements)
        c. [T] Implement password hashing and secure login logic to protect user data.
    2. [U]
        a. [T]

## Initial Architecture Envisioning
Frontend: ASP.NET Core MVC with Bootstrap (Mobile-responsive).
Backend Logic: C# Service layer for Proximity Scoring.
Database: SQL Server (Azure hosted).
External APIs: Google Maps Platform API, OpenAI Vision API.
        
## Agile Data Modeling
Diagrams, SQL modeling (dbdiagram.io), UML diagrams

## Timeline and Release Plan
Sprint Cadence: 2 weeks Sprints (with weekly updates).
Release Plan: Git Flow. Feature development will happen on dedicated feature branches that are merged into dev and will be approved via PR's and deployed to main the end of each sprint

Milestone,Date,Description
Milestone 1,Jan 14,Project Selection & Initial Setup
Milestone 2,Jan 21,Inception Phase & Requirements Documentation
Milestone 3,Jan 28,Initial Prototype & Architecture Implementation
Milestone 4,Feb 4,Core Gameplay Loop & API Integration

Sprint Schedule,Date
Sprint 1, Feb 16 - Mar 1
Sprint 2, Mar 2 - Mar 15
Sprint 3, Mar 16 - Mar 30
    