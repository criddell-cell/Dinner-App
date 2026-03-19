# DinnerPicker — Architecture Document
**Architect Artifact | v1.0**

---

## Project Type: Console Application (.NET 10)

**Justification:**
The UX Designer correctly identified a console app as the right fit. .NET 10 console apps support:
- Top-level statements (clean Program.cs)
- System.Text.Json (built-in, no extra packages needed for JSON persistence)
- HttpClient (built-in, for Claude API calls)
- ANSI terminal escape codes (for color/formatting on macOS/Windows Terminal)

No external UI framework, no NuGet UI packages. Lean dependency graph.

**Required NuGet packages:**
- `Microsoft.Extensions.DependencyInjection` — lightweight DI container
- *(All other dependencies are part of .NET base class library)*

---

## Solution & Project Structure

```
DinnerPicker/
├── DinnerPicker.sln
├── docs/                          # Architecture & design docs (this file, etc.)
├── src/
│   └── DinnerPicker/
│       ├── DinnerPicker.csproj
│       ├── Program.cs             # Entry point; DI setup; main application loop
│       │
│       ├── Models/
│       │   ├── AppData.cs         # Root JSON persistence object
│       │   ├── QuizAnswers.cs     # Typed results from the quiz session
│       │   └── MealSuggestion.cs  # Suggestion returned by Claude
│       │
│       ├── Services/
│       │   ├── IPantryService.cs
│       │   ├── PantryService.cs   # Pantry staples + fridge management
│       │   ├── IQuizService.cs
│       │   ├── QuizService.cs     # Adaptive quiz engine
│       │   ├── ISuggestionService.cs
│       │   ├── SuggestionService.cs  # Claude API integration
│       │   ├── IHistoryService.cs
│       │   └── HistoryService.cs  # Meal history tracking
│       │
│       ├── Persistence/
│       │   ├── IDataStore.cs
│       │   └── JsonDataStore.cs   # Read/write AppData to ~/.dinnerpicker/
│       │
│       └── UI/
│           └── ConsoleUI.cs       # All terminal rendering (screens, cards, menus)
│
└── tests/
    └── DinnerPicker.Tests/
        ├── DinnerPicker.Tests.csproj
        ├── PantryServiceTests.cs
        ├── QuizServiceTests.cs
        └── SuggestionServiceTests.cs
```

---

## Core Data Models

### AppData (Persistence Root)
```csharp
// Serialized as: ~/.dinnerpicker/appdata.json
class AppData
{
    bool IsFirstRun { get; set; }           // false after onboarding complete
    List<string> PantryStaples { get; set; } // Persistent staple ingredients
    List<MealHistoryEntry> MealHistory { get; set; } // Rolling 30-entry log
}
```

### MealHistoryEntry
```csharp
class MealHistoryEntry
{
    DateTime Date { get; set; }
    List<string> SuggestedMeals { get; set; } // Names of the 3 suggestions
}
```

### QuizAnswers
```csharp
class QuizAnswers
{
    string EnergyLevel { get; set; }      // "low" | "medium" | "high"
    string TimeAvailable { get; set; }    // "15" | "30" | "45"
    string CravingDirection { get; set; } // "familiar" | "adventurous" | "surprise"
    string CuisinePreference { get; set; }// "mexican" | "asian" | "italian" | "american" | "any"
    string? Restriction { get; set; }     // "no-meat" | "no-carbs" | "no-dairy" | null
}
```

### MealSuggestion
```csharp
class MealSuggestion
{
    string Name { get; set; }
    string Description { get; set; }
    string Cuisine { get; set; }
    int CookTimeMinutes { get; set; }
    List<string> AvailableIngredients { get; set; }
    List<string> AdditionalIngredients { get; set; } // Empty for options 1+2, ≤3 for option 3
}
```

---

## Service Boundaries

### IPantryService / PantryService
- Holds current session's fridge contents (in-memory)
- Reads/writes pantry staples via IDataStore
- Provides `GetAllIngredients()` → combined pantry + fridge list

### IQuizService / QuizService
- Stateless: takes Console I/O and returns a filled `QuizAnswers`
- Implements the adaptive branching logic (energy → time → craving → Q4A/Q4B → Q5)
- Delegates all rendering to ConsoleUI

### ISuggestionService / SuggestionService
- Constructs the Claude API prompt from pantry contents + quiz answers + history
- Makes HTTP POST to `https://api.anthropic.com/v1/messages`
- Deserializes the JSON response into `List<MealSuggestion>`
- Throws `SuggestionException` on API failure (caught by Program.cs)

### IHistoryService / HistoryService
- Reads/writes `MealHistoryEntry` list via IDataStore
- `GetRecentMeals(int count)` → returns last N meal names for prompt context
- `RecordSession(List<MealSuggestion>)` → appends and trims to 30 entries

### IDataStore / JsonDataStore
- Single file: `~/.dinnerpicker/appdata.json`
- `Load()` → deserializes AppData (returns default if file not found)
- `Save(AppData)` → serializes to disk with indented JSON

---

## Suggestion Engine: Hybrid Rule + AI

**Approach:** AI-primary with rule-based prompt engineering.

The suggestion engine does NOT attempt local rule-based meal matching (too fragile, too much meal data to maintain). Instead:

1. **Prompt construction (rule-based):** Program assembles a structured context payload:
   - Pantry staples list
   - Fridge contents list
   - Recent meal history (last 5)
   - Quiz answers (energy, time, cuisine preference, restrictions)
   - Hard constraints (2 "have it" meals, 1 "buy 3 things" meal)

2. **Claude API call:** POST to `claude-sonnet-4-20250514` with structured system prompt + user message

3. **Response parsing (rule-based):** Parse JSON from Claude's response; validate that:
   - Exactly 3 suggestions returned
   - Suggestions 1 & 2 have empty `AdditionalIngredients`
   - Suggestion 3 has ≤ 3 `AdditionalIngredients`

4. **Retry on invalid response:** If validation fails, retry once with a stricter prompt addendum

**Claude API Request Shape:**
```json
{
  "model": "claude-sonnet-4-20250514",
  "max_tokens": 1024,
  "system": "<system prompt>",
  "messages": [
    { "role": "user", "content": "<structured context>" }
  ]
}
```

**System Prompt:**
```
You are a personal dinner suggestion assistant for a solo home cook.
Return ONLY valid JSON. No explanation text. No markdown. Just raw JSON.
Always return exactly 3 suggestions in the format specified.
```

---

## Local Data Persistence

**Strategy:** Single JSON file at `~/.dinnerpicker/appdata.json`

**Why JSON over SQLite:**
- No schema migrations to manage
- Human-readable (user can inspect/edit if needed)
- Zero additional dependencies
- Sufficient for this data volume (pantry ≤ 50 items, history ≤ 30 entries)

**File path resolution:**
```csharp
Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".dinnerpicker",
    "appdata.json"
)
```

---

## Dependency Injection Setup (Program.cs)

```csharp
var services = new ServiceCollection()
    .AddSingleton<IDataStore, JsonDataStore>()
    .AddSingleton<IPantryService, PantryService>()
    .AddSingleton<IHistoryService, HistoryService>()
    .AddTransient<IQuizService, QuizService>()
    .AddTransient<ISuggestionService, SuggestionService>()
    .AddSingleton<ConsoleUI>()
    .AddHttpClient() // IHttpClientFactory
    .BuildServiceProvider();
```

---

## Configuration & Environment

| Setting | Source |
|---------|--------|
| `ANTHROPIC_API_KEY` | Environment variable (required) |
| Data directory | `~/.dinnerpicker/` (auto-created on first run) |
| Model ID | Hardcoded constant: `claude-sonnet-4-20250514` |
| History limit | Hardcoded constant: 30 entries |
| Recent meal context | Hardcoded constant: last 5 meals |
