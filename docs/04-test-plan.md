# DinnerPicker — Test Plan
**QA Artifact | v1.0**

---

## Scope
Full test coverage for DinnerPicker v1.0, covering unit tests, integration scenarios, and manual acceptance testing. Automated tests are xUnit + Moq. Manual tests cover terminal UX flows.

---

## Test Categories

### 1. Happy Path Flows

| ID | Scenario | Steps | Expected Result |
|----|----------|-------|-----------------|
| HP-01 | First-time setup | Launch with no appdata.json | Welcome screen shown; pantry setup entered; defaults pre-loaded |
| HP-02 | Standard nightly session | Log fridge contents → complete quiz → receive suggestions | 3 suggestions returned; 2 with no additional ingredients; 1 with ≤3 |
| HP-03 | Returning user skips setup | Launch with existing appdata.json | Main menu shown directly; no onboarding |
| HP-04 | Suggestion accepted + history saved | Complete a session successfully | Meal names appear in history screen; appdata.json updated on disk |
| HP-05 | Pantry edited mid-use | Add item via Manage Pantry → start session | New item appears in ingredient list passed to Claude |

---

### 2. Edge Cases

| ID | Scenario | Steps | Expected Result |
|----|----------|-------|-----------------|
| EC-01 | Empty fridge | Press ENTER at fridge input | App proceeds with pantry only; "working with pantry staples only" shown |
| EC-02 | All cuisines recently used | Populate history with all 4 cuisine types across last 5 sessions | Claude still returns results; least-recently-used cuisine prioritized |
| EC-03 | Low energy + short time quiz path | Select "Low" energy → only 2 time options shown; Q5 skipped | QuizAnswers.Restriction = null; TimeAvailable = "15" or "30" only |
| EC-04 | Duplicate pantry entry | Add "garlic" when garlic already exists | "Already in your pantry" message shown; no duplicate added |
| EC-05 | Empty pantry (edge) | Remove all staples then start session | Session proceeds; Claude generates from empty pantry + fridge |
| EC-06 | Single-item fridge | Enter one ingredient | Parsed and passed correctly; no crash |
| EC-07 | Quiz restart | Press R on question 3 | Quiz resets to Q1; previous answers discarded |

---

### 3. Pantry Persistence

| ID | Scenario | Expected Result |
|----|----------|-----------------|
| PP-01 | Add staple, restart app | Added item present in pantry on relaunch |
| PP-02 | Remove staple, restart app | Removed item absent from pantry on relaunch |
| PP-03 | Reset to defaults, restart app | Default 14 staples present; custom additions gone |
| PP-04 | First run flag cleared after onboarding | Second launch shows main menu, not welcome screen |

---

### 4. Suggestion Validation (2+1 Rule)

| ID | Check | Method |
|----|-------|--------|
| SV-01 | Exactly 3 suggestions returned | Assert `suggestions.Count == 3` |
| SV-02 | Suggestion 1 has no additional ingredients | Assert `suggestions[0].AdditionalIngredients.Count == 0` |
| SV-03 | Suggestion 2 has no additional ingredients | Assert `suggestions[1].AdditionalIngredients.Count == 0` |
| SV-04 | Suggestion 3 has 1–3 additional ingredients | Assert count is between 1 and 3 inclusive |
| SV-05 | No two suggestions share the same cuisine | Assert distinct cuisine values across all 3 |
| SV-06 | Cook time respects quiz time answer | Assert all CookTimeMinutes ≤ stated time |

---

### 5. Claude API Failure Handling

| ID | Scenario | Expected Result |
|----|----------|-----------------|
| AF-01 | API returns 401 Unauthorized | SuggestionException thrown; error screen shown |
| AF-02 | API returns 500 Server Error | SuggestionException thrown; retry offered |
| AF-03 | Network unreachable (HttpRequestException) | Friendly error shown; app does not crash |
| AF-04 | Claude returns malformed JSON | Retry attempt made with stricter prompt; exception on second failure |
| AF-05 | Claude returns wrong number of suggestions | ParseAndValidate returns null; retry triggered |
| AF-06 | Claude returns suggestions[0] with additionalIngredients | Validation fails; retry with strict prompt |
| AF-07 | ANTHROPIC_API_KEY not set at launch | Startup warning shown; app exits gracefully before session |
| AF-08 | User selects "Return to main menu" on error | No history entry saved; main menu shown cleanly |

---

## Test Environment Setup

```bash
# Run all unit tests
cd /Users/courtneyriddell/DinnerPicker
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "ClassName=PantryServiceTests"
```

---

## Manual Acceptance Checklist

- [ ] First launch: welcome screen shown, pantry defaults pre-loaded
- [ ] Pantry edit: add/remove/reset all work and persist across restart
- [ ] Fridge empty path: no crash, session proceeds
- [ ] Quiz low-energy path: only 2 time options shown, Q5 not shown
- [ ] Quiz restart: R key resets to Q1 from any question
- [ ] Suggestions: correct 2+1 layout rendered with card borders
- [ ] Option 3: "Also pick up" items highlighted in yellow
- [ ] History screen: most recent session shown at top
- [ ] API key missing: startup error shown, no crash
- [ ] API failure during session: retry/back-to-menu offered
