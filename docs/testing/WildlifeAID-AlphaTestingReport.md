# Wildlife AID Alpha Testing  Feedback Report
## Session I - April 22, 2026
*Generated from Team Feedback Forms using ChatGPT & Gemini*

### 1. Bugs Identified

**Bug 1: Al Bot unavailable on first attempt**
1. Starting condition: User is logged in and accesses Al Bot.
2. Steps: Navigate to Al Bot and send initial message.
3. Actual: Generic connection error (Gemini API).
4. Expected: Successful response on first attempt.
5. Remarks: Works on subsequent attempts; likely initialization issue.

**Bug 2: Email verification resend does not work**
1. Starting condition: User with unverified email.
2. Steps: Attempt login → click resend verification.
3. Actual: No email or silent failure.
4. Expected: Email successfully resent.
5. Remarks: Blocks onboarding.

**Bug 3: Login flow loops for unverified users**
1. Starting condition: Login attempt with unverified email.
2. Steps: Attempt login → prompted to verify.
3. Actual: User stuck in loop.
4. Expected: Clear path to verify then login.
5. Remarks: Likely tied to verification system.

**Bug 4: Latitude & Longitude do not auto-populate**
1. Starting condition: Creating sighting.
2. Steps: Upload or create sighting.
3. Actual: Coordinates missing.
4. Expected: Auto-populated fields.
5. Remarks: Impacts usability.

**Bug 5: Register form infinite loading with invalid password**
1. Starting condition: Invalid password during registration.
2. Steps: Submit registration form.
3. Actual: Button locks with spinner.
4. Expected: Validation error shown, button re-enabled.
5. Remarks: Critical UX issue.

***Bugs List Key***
1. Starting condition, setup or scenario
2. Steps to replicate the failure
3. Actual result
4. Expected result
5. Remarks

### 2. Recommendations

**Account/Dashboard UX**
* Move settings under account icon
* Remove account settings from dashboard
* Align with standard UI practices

**Sightings Gallery**
* Make entries clickable for details
* Or indicate non-interactive state clearly

**Al Bot Improvements**
* Fix first interaction failure
* Improve API reliability
* Add clearer error messages

**Form Validation**
* Add real-time password validation
* Provide clear error feedback
* Prevent UI lock states

### 3. General Feedback

**Strengths**
* Strong feature set
* Intuitive overall concept

**Pain Points**
* Inconsistent error feedback
* Misleading UI elements
* Incomplete features
* Unreliable critical flows

### 4. Summary of Key Issues
1. **Blocking:** Registration form, Email verification
2. **High-impact UX:** AI Bot failure, Gallery interaction
3. **Reliability:** Location auto-population
