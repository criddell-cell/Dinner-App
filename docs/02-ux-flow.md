# DinnerPicker — UX Flow & Screen Specification
**UX Designer Artifact | v1.0**

---

## Platform Recommendation: Console Application (C# Console App)

**Justification:**
This app is used by a single person, once per evening, for a 2-minute interaction. A console app is:
- Instantly launchable (no window management)
- Zero UI framework overhead
- Perfectly suited to a short, linear quiz flow
- Easily scriptable (can be added to a terminal startup alias)
- Cross-platform via .NET

A GUI (WPF/MAUI) would be over-engineered for this use case. The key UX investment is in *clear copy, fast navigation, and visually clean terminal output* — not widgets.

**Visual style:** Use ANSI color codes for headers, highlights, and suggestion cards. Box-drawing characters for cards. Numbered menus throughout for fast keyboard navigation.

---

## Screen Map (State Diagram)

```
[LAUNCH]
    │
    ├─ First run? ──YES──► [WELCOME + ONBOARDING]
    │                              │
    │                        [PANTRY SETUP]
    │                              │
    └─ Returning? ─────────► [MAIN MENU]
                                   │
               ┌───────────────────┼────────────────────┐
               │                   │                    │
       [START SESSION]    [MANAGE PANTRY]         [VIEW HISTORY]
               │
       [FRIDGE INPUT]
               │
         [QUIZ: Q1] ──► [Q2] ──► [Q3] ──► [Q4*] ──► [Q5*]
               │         (* = conditional)
       [GENERATING...]
               │
       [SUGGESTIONS DISPLAY]
               │
          [MAIN MENU]
```

---

## Screen Specifications

---

### SCREEN 1: Welcome (First Run Only)

**Trigger:** No `appdata.json` found on disk.

**Display:**
```
╔══════════════════════════════════════════════════╗
║           🍽  DINNER PICKER                      ║
║          Your personal evening meal planner      ║
╚══════════════════════════════════════════════════╝

  Welcome! I'll help you decide what to cook tonight.

  Each evening, answer a quick 4–5 question quiz and
  get 3 personalized dinner suggestions based on what
  you already have — healthy, home-cooked, fast.

  Let's start by setting up your pantry staples.
  (You'll only need to do this once.)

  Press ENTER to continue...
```

**Next:** → SCREEN 2: Pantry Setup

---

### SCREEN 2: Pantry Setup

**Trigger:** First run OR "Manage Pantry Staples" from main menu.

**Display:**
```
╔══════════════════════════════════════════════════╗
║  PANTRY STAPLES                                  ║
╚══════════════════════════════════════════════════╝

  These are ingredients always assumed to be in your
  kitchen. Edit this list whenever your staples change.

  Current staples (14 items):
  ─────────────────────────────────────────────────
   1. olive oil          8.  soy sauce
   2. garlic             9.  canned tomatoes
   3. onion             10.  salt & pepper
   4. pasta             11.  butter
   5. rice              12.  flour
   6. eggs              13.  vegetable broth
   7. lemon             14.  hot sauce
  ─────────────────────────────────────────────────

  [A] Add ingredient
  [R] Remove ingredient (enter number)
  [D] Reset to defaults
  [S] Save and continue

  Choice: _
```

**Interaction:**
- `A` → prompts "Add ingredient: " — accepts free text — adds to list
- `R` → prompts "Remove item number: " — removes that entry
- `D` → resets to the default 14 staples (confirms first)
- `S` → saves to disk, proceeds to MAIN MENU (or session if coming from flow)

**Validation:**
- Duplicate check (case-insensitive): "Garlic already in your pantry."
- Empty input: re-prompts silently

---

### SCREEN 3: Main Menu

**Trigger:** Every time after pantry setup, or after a session ends.

**Display:**
```
╔══════════════════════════════════════════════════╗
║           🍽  DINNER PICKER                      ║
╚══════════════════════════════════════════════════╝

  What would you like to do?

  [1] Start tonight's dinner session
  [2] Manage pantry staples
  [3] View recent meal history
  [Q] Quit

  Choice: _
```

---

### SCREEN 4: Fridge Input

**Trigger:** User selects [1] Start tonight's dinner session.

**Display:**
```
╔══════════════════════════════════════════════════╗
║  WHAT'S IN YOUR FRIDGE?                          ║
╚══════════════════════════════════════════════════╝

  Enter your fresh ingredients (comma-separated),
  or press ENTER to skip if your fridge is empty.

  Examples: chicken breast, spinach, cherry tomatoes

  Fridge contents: _
```

**After entry:**
```
  Got it! Here's everything I'm working with:

  Pantry (14): olive oil, garlic, onion, pasta, rice,
               eggs, soy sauce, canned tomatoes, salt &
               pepper, butter, flour, broth, lemon, hot sauce

  Fridge (3):  chicken breast, spinach, cherry tomatoes

  Press ENTER to start the quiz...
```

**Edge case — empty fridge:**
- User presses ENTER with no input
- Confirmation: "No problem — I'll work with your pantry staples only."

---

### SCREEN 5: Quiz

**Trigger:** User confirms ingredient summary and presses ENTER.

**Quiz architecture — 4–5 questions, one per screen:**

---

#### Q1: Energy Level

```
╔══════════════════════════════════════════════════╗
║  TONIGHT'S DINNER QUIZ   ●○○○○                   ║
╚══════════════════════════════════════════════════╝

  Question 1 of ~4

  How's your energy level tonight?

  [1] Low — I want something cozy and minimal effort
  [2] Medium — I can handle a normal cook
  [3] High — I'm actually excited to cook tonight

  Choice: _
```

---

#### Q2: Time Available

```
╔══════════════════════════════════════════════════╗
║  TONIGHT'S DINNER QUIZ   ●●○○○                   ║
╚══════════════════════════════════════════════════╝

  Question 2 of ~4

  How much time do you have?

  [1] 15–20 minutes — quick and done
  [2] 30 minutes — the usual
  [3] 45+ minutes — I've got time tonight

  Choice: _
```

**Adaptive rule:** If Q1 = Low, option [3] is hidden. Only 2 options shown.

---

#### Q3: Craving Direction

```
╔══════════════════════════════════════════════════╗
║  TONIGHT'S DINNER QUIZ   ●●●○○                   ║
╚══════════════════════════════════════════════════╝

  Question 3 of ~4

  What kind of meal sounds good tonight?

  [1] Familiar & comforting — something I know I love
  [2] A little adventurous — mix it up a bit
  [3] Surprise me — I'm open to anything

  Choice: _
```

---

#### Q4A: Comfort Type (if Q3 = Familiar)

```
╔══════════════════════════════════════════════════╗
║  TONIGHT'S DINNER QUIZ   ●●●●○                   ║
╚══════════════════════════════════════════════════╝

  Question 4 of ~4

  What kind of comfort food sounds right?

  [1] Pasta or something Italian
  [2] Rice bowl or something Asian
  [3] Tacos or something Mexican
  [4] Hearty North American (burgers, soup, grain bowls)

  Choice: _
```

---

#### Q4B: Cuisine Preference (if Q3 = Adventurous or Surprise me)

```
╔══════════════════════════════════════════════════╗
║  TONIGHT'S DINNER QUIZ   ●●●●○                   ║
╚══════════════════════════════════════════════════╝

  Question 4 of ~4

  Any cuisine calling your name?

  [1] Mexican / Latin American
  [2] Asian (Japanese, Thai, Chinese, Korean...)
  [3] Italian / Mediterranean
  [4] North American comfort
  [5] No preference — surprise me

  Choice: _
```

---

#### Q5 (Conditional — only if time ≥ 30 min):

```
╔══════════════════════════════════════════════════╗
║  TONIGHT'S DINNER QUIZ   ●●●●●                   ║
╚══════════════════════════════════════════════════╝

  One last question!

  Anything you're NOT in the mood for tonight?

  [1] No meat tonight
  [2] No heavy carbs
  [3] No dairy
  [4] Nope, I'm good with anything

  Choice: _
```

---

### SCREEN 6: Generating

**Trigger:** Quiz complete.

**Display:**
```
╔══════════════════════════════════════════════════╗
║  FINDING YOUR DINNER...                          ║
╚══════════════════════════════════════════════════╝

  Checking your pantry and fridge...
  Running your quiz answers through the suggestion engine...

  (This takes a few seconds)
```

**Error state (API failure):**
```
  ⚠  Couldn't reach the suggestion engine.

  [1] Try again
  [2] Return to main menu

  Choice: _
```

---

### SCREEN 7: Suggestions Display

**Trigger:** Claude API returns 3 valid suggestions.

**Display:**
```
╔══════════════════════════════════════════════════╗
║  TONIGHT'S DINNER OPTIONS                        ║
╚══════════════════════════════════════════════════╝

  ✅ OPTION 1 — YOU HAVE EVERYTHING   [Asian · 25 min]
  ┌─────────────────────────────────────────────────┐
  │  Garlic Soy Chicken Rice Bowl                   │
  │                                                 │
  │  A savory weeknight staple — pan-seared chicken │
  │  glazed with soy, garlic, and a splash of broth │
  │  served over fluffy rice.                       │
  │                                                 │
  │  Ingredients: chicken breast, soy sauce, garlic,│
  │  rice, vegetable broth, olive oil               │
  └─────────────────────────────────────────────────┘

  ✅ OPTION 2 — YOU HAVE EVERYTHING   [Italian · 20 min]
  ┌─────────────────────────────────────────────────┐
  │  Cherry Tomato Pasta with Wilted Spinach        │
  │                                                 │
  │  Simple and bright — pasta tossed with burst    │
  │  cherry tomatoes, spinach, garlic, and lemon.   │
  │                                                 │
  │  Ingredients: pasta, cherry tomatoes, spinach,  │
  │  garlic, olive oil, lemon                       │
  └─────────────────────────────────────────────────┘

  🛒 OPTION 3 — GRAB 3 THINGS         [Mexican · 30 min]
  ┌─────────────────────────────────────────────────┐
  │  Chicken Burrito Bowl with Avocado Crema        │
  │                                                 │
  │  A fresh, filling bowl with seasoned chicken,   │
  │  rice, and a simple avocado crema.              │
  │                                                 │
  │  You have: chicken breast, rice, garlic, onion, │
  │  hot sauce                                      │
  │                                                 │
  │  Also pick up: avocado, black beans, lime       │
  └─────────────────────────────────────────────────┘

  ─────────────────────────────────────────────────
  Press ENTER to return to the main menu.
```

---

### SCREEN 8: Meal History

**Trigger:** User selects [3] from main menu.

**Display:**
```
╔══════════════════════════════════════════════════╗
║  RECENT MEAL HISTORY                             ║
╚══════════════════════════════════════════════════╝

  Thu Mar 14  Garlic Soy Chicken Rice Bowl
              Cherry Tomato Pasta with Wilted Spinach
              Chicken Burrito Bowl with Avocado Crema

  Wed Mar 13  ...

  Press ENTER to return to main menu.
```

---

## Edge Case Handling Summary

| Situation | Behavior |
|-----------|----------|
| First run, no data file | Onboarding flow → Pantry Setup → Main Menu |
| Empty fridge | Proceed with pantry only; confirm message shown |
| API unavailable | Friendly error with Retry / Back to menu |
| Missing API key | Startup warning with setup instructions |
| No meal history yet | History screen shows "No sessions yet." |
| All cuisines recently used | Claude is still given history; instructed to pick least-recent |
| Quiz restarted mid-way | [R] Restart option on any quiz screen resets to Q1 |
