# DinnerPicker — Product Requirements & User Stories
**Product Owner Artifact | v1.0**

---

## Vision Statement
A solo home cook's evening companion that removes decision fatigue through a quick mood check-in, then delivers exactly 3 personalized, healthy dinner suggestions — 2 from what's already in the kitchen, 1 that needs a quick grocery run.

---

## Personas
**Primary: The Solo Cook**
- Cooks only for themselves
- Values speed and low mental overhead after a workday
- Wants healthy, home-cooked meals — no takeout
- Has consistent pantry staples and rotates fresh ingredients weekly

---

## Epics & User Stories

---

### EPIC 1 — Pantry Staples Management

**US-01: Set Up Pantry Staples (First Run)**
> As a first-time user, I want to configure my permanent pantry staples so the app always knows what I have on hand without me re-entering it every session.

**Acceptance Criteria:**
- On first launch, the app prompts the user to set up their pantry
- A default set of common staples is pre-loaded (olive oil, garlic, onion, pasta, rice, eggs, soy sauce, canned tomatoes, salt, pepper, butter, flour, vegetable broth)
- User can add, remove, or accept defaults
- Pantry staples are saved to disk and persist across sessions

---

**US-02: Edit Pantry Staples**
> As a returning user, I want to occasionally update my pantry staples list so the suggestions stay accurate as my cooking style evolves.

**Acceptance Criteria:**
- Main menu exposes a "Manage Pantry Staples" option
- User can add or remove individual items
- Changes are saved immediately to disk
- Confirmation message shown after saving

---

### EPIC 2 — Fridge Inventory Input

**US-03: Log Tonight's Fresh Ingredients**
> As a user starting an evening session, I want to quickly tell the app what fresh ingredients I have in the fridge so suggestions are grounded in reality.

**Acceptance Criteria:**
- At session start, user is prompted for fridge contents
- User enters ingredients as a comma-separated list or one at a time
- An "empty fridge" option is available (skips entry)
- Fridge contents are not persisted — they're session-only
- The total combined ingredient list (pantry + fridge) is displayed as confirmation before the quiz begins

---

### EPIC 3 — Mood-Based Quiz

**US-04: Take the Evening Mood Quiz**
> As a user, I want to answer a short adaptive quiz each evening so the suggestions match how I actually feel tonight.

**Acceptance Criteria:**
- Quiz contains 3–5 questions, displayed one at a time
- Questions cover: energy level, available time, craving direction, cuisine preference
- At least one question adapts based on a prior answer (see UX spec for logic)
- User selects answers via numbered menu options — no free-text input
- User can restart the quiz before submission
- Quiz cannot be skipped — it must be completed before suggestions are shown

---

**US-05: Adaptive Quiz Logic**
> As a user with low energy, I want the quiz to shortcut to comfort food suggestions rather than asking me about adventurous cuisine options.

**Acceptance Criteria:**
- If energy level = Low, cuisine preference question defaults to "comfort food" and the adventurous cuisine question is skipped
- If craving = Familiar, question 4 asks about comfort food type
- If craving = Adventurous or New Territory, question 4 asks about cuisine region
- A fifth "any ingredient to avoid tonight?" question appears only if time ≥ 30 min

---

### EPIC 4 — Dinner Suggestion Engine

**US-06: Receive 3 Dinner Suggestions**
> As a user who has completed the quiz, I want to see exactly 3 dinner suggestions with clear ingredient and time information so I can make a decision quickly.

**Acceptance Criteria:**
- Exactly 3 suggestions are always returned
- Suggestion 1 and 2 use ONLY ingredients from the combined pantry + fridge list
- Suggestion 3 requires no more than 3 additional ingredients to purchase
- Each suggestion shows: name, description, cuisine type, estimated cook time, ingredient list, and (for suggestion 3) what to buy
- Suggestions are generated via Claude API (model: claude-sonnet-4-20250514)
- If the API call fails, a graceful error message is shown with a retry option

---

**US-07: Cuisine Variety**
> As a user, I want the 3 suggestions to span different cuisines so I'm not offered three pasta dishes.

**Acceptance Criteria:**
- No two suggestions share the same cuisine category
- Cuisine categories: Mexican/Latin, Asian, Italian/Mediterranean, North American
- The prompt instructs Claude to diversify across these four categories
- Recent meal history (last 5 meals) is passed to Claude to avoid repeating recently suggested meals

---

**US-08: Health & Speed Constraints**
> As a health-conscious user in a hurry, I want all suggestions to be healthy and include an estimated cook time.

**Acceptance Criteria:**
- All suggestions must be home-cooked (no mention of takeout, delivery, or restaurants)
- Cook time is surfaced per suggestion (in minutes)
- The prompt instructs Claude to prioritize nutritionally balanced, whole-food meals

---

### EPIC 5 — Session History & Variety

**US-09: Track Recent Meal History**
> As a returning user, I want the app to remember what I've been eating recently so it doesn't keep suggesting the same meals.

**Acceptance Criteria:**
- Each time a session completes (suggestions returned), the 3 suggested meal names are saved to meal history
- History stores up to 30 entries (rolling)
- The 5 most recent meals are included in the Claude API context
- History is persisted to disk and survives app restarts

---

### EPIC 6 — First-Run & Edge Cases

**US-10: First-Time Setup Experience**
> As a brand new user, I want a friendly onboarding flow that gets me set up quickly without being overwhelming.

**Acceptance Criteria:**
- App detects first run (no data file exists)
- Welcomes user and explains the app in 2–3 lines
- Guides user through pantry setup before anything else
- Does not require fridge contents on first run (can proceed with pantry only)

---

**US-11: Empty Fridge Handling**
> As a user with nothing fresh in the fridge, I want the app to still work and generate suggestions based purely on pantry staples.

**Acceptance Criteria:**
- User can select "My fridge is empty" at the fridge input step
- Claude is informed that only pantry staples are available
- All 3 suggestions are generated; the first 2 still use only pantry staples, the 3rd requires ≤3 new ingredients
- No error or failure state occurs

---

**US-12: API Failure Graceful Handling**
> As a user, if the Claude API is unavailable, I want a clear message and the option to retry rather than a crash.

**Acceptance Criteria:**
- If the HTTP request fails or returns an error status, the app shows a friendly error message
- User is offered: [1] Retry  [2] Return to main menu
- App does not crash or show a raw stack trace
- If ANTHROPIC_API_KEY is missing, app shows a setup instruction message on launch

---

## Prioritized Backlog (MoSCoW)

| Priority | Story | Rationale |
|----------|-------|-----------|
| Must | US-01, US-03, US-04, US-06 | Core loop — app cannot function without these |
| Must | US-10, US-12 | First-run and failure handling are table stakes |
| Should | US-02, US-07, US-08, US-09 | Quality and variety — needed for a good daily experience |
| Should | US-05, US-11 | Adaptive quiz and edge case handling |
| Could | US-13 (future: export shopping list) | Nice to have, out of scope v1 |
