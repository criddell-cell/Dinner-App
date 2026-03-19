using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DinnerPicker.Models;

namespace DinnerPicker.Services;

/// <summary>
/// Calls the Claude API to generate exactly 3 dinner suggestions.
/// Constructs a structured prompt from ingredients + quiz answers + history,
/// then validates the JSON response before returning.
/// </summary>
public class SuggestionService : ISuggestionService
{
    private const string ModelId = "claude-sonnet-4-20250514";
    private const string ApiEndpoint = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    public SuggestionService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? string.Empty;
    }

    public async Task<List<MealSuggestion>> GetSuggestionsAsync(
        IReadOnlyList<string> allIngredients,
        QuizAnswers quiz,
        List<string> recentMealNames,
        IReadOnlyList<string> exclusions)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new SuggestionException(
                "ANTHROPIC_API_KEY is not set. Add it to your environment and restart the app.");

        var userMessage = BuildUserMessage(allIngredients, quiz, recentMealNames, exclusions);

        for (int attempt = 1; attempt <= 2; attempt++)
        {
            var responseText = await CallClaudeApiAsync(userMessage, strict: attempt == 2);
            var suggestions = ParseAndValidate(responseText);
            if (suggestions != null) return suggestions;
        }

        throw new SuggestionException(
            "The suggestion engine returned an invalid response after 2 attempts.");
    }

    // ── Prompt construction ───────────────────────────────────────────────────

    private static string BuildSystemPrompt(bool strict = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a personal dinner suggestion assistant for a solo home cook.");
        sb.AppendLine("All meals must be home-cooked and realistic for one person.");
        sb.AppendLine("No takeout, no delivery, no restaurant recommendations.");
        sb.AppendLine();
        sb.AppendLine("STRUCTURAL RULES:");
        sb.AppendLine("1. Return EXACTLY 3 dinner suggestions.");
        sb.AppendLine("2. Suggestions 1 and 2: 'missingIngredients' MUST be an empty array [].");
        sb.AppendLine("   Use ONLY ingredients from the available ingredients list.");
        sb.AppendLine("3. Suggestion 3: 'missingIngredients' must have 1–3 items the user needs to buy.");
        sb.AppendLine("4. No two suggestions may share the same cuisine.");
        sb.AppendLine("5. Avoid any meal listed in the recent history.");
        sb.AppendLine("6. Only suggest recipes with an estimated average online rating of 3.5 out of 5 or higher.");
        sb.AppendLine("   Base this on your knowledge of how well-regarded the dish is on popular recipe sites.");
        sb.AppendLine("   Include the rating as a 'rating' field (number between 3.5 and 5.0, one decimal place).");
        sb.AppendLine();
        sb.AppendLine("QUIZ RULES — apply ALL of these strictly:");
        sb.AppendLine();
        sb.AppendLine("TIME: 'estimatedMinutes' must not exceed the stated max. Be honest — include prep time.");
        sb.AppendLine();
        sb.AppendLine("ENERGY LEVEL:");
        sb.AppendLine("  low    → simple oven or stovetop meals (roasting, tray bakes, one-pan stove meals).");
        sb.AppendLine("           Minimal chopping and technique. All 3 suggestions must need minimal or no grocery shopping.");
        sb.AppendLine("  medium → straightforward cooking. A couple of steps, basic techniques.");
        sb.AppendLine("  high   → more involved meals are fine. Multiple components, active cooking welcome.");
        sb.AppendLine();
        sb.AppendLine("HEALTH PREFERENCE:");
        sb.AppendLine("  clean    → strictly low carb. No pasta, rice, bread, potatoes, grains, or sugary sauces.");
        sb.AppendLine("             Meals must be built around protein and non-starchy vegetables.");
        sb.AppendLine("  balanced → healthy but satisfying. Lean proteins, whole grains, veg-forward.");
        sb.AppendLine("             Moderate portions of carbs and fat are fine.");
        sb.AppendLine("  comfort  → hearty and indulgent. Pasta, cheese, rich sauces, fried elements all welcome.");
        sb.AppendLine("             Prioritise satisfaction over nutritional purity.");
        sb.AppendLine();
        sb.AppendLine("CRAVING DIRECTION:");
        sb.AppendLine("  comfort  → classic, familiar dishes the user already knows and loves.");
        sb.AppendLine("  mix      → a twist on something familiar, or a straightforward global dish.");
        sb.AppendLine("  surprise → bold, unexpected, or creative combinations. Push outside the obvious.");
        sb.AppendLine();
        sb.AppendLine("CUISINE PREFERENCE:");
        sb.AppendLine("  If a specific cuisine is given, at least 2 of the 3 suggestions must match it.");
        sb.AppendLine("  If 'any', spread suggestions across different cuisines.");
        sb.AppendLine();
        sb.AppendLine("Return ONLY raw JSON — no markdown fences, no explanation.");
        sb.AppendLine("Use this exact structure:");
        sb.AppendLine("""
{
  "suggestions": [
    {
      "id": 1,
      "name": "Meal Name",
      "cuisine": "Asian",
      "estimatedMinutes": 20,
      "healthNotes": "High protein, low carb",
      "appliance": "Stovetop",
      "equipment": "Large frying pan",
      "ingredients": ["ingredient1", "ingredient2"],
      "missingIngredients": [],
      "rating": 4.5,
      "instructions": "One or two sentence description shown on the card.",
      "recipeSteps": [
        "Heat oil in a large frying pan over medium heat.",
        "Add garlic and cook for 1 minute until fragrant.",
        "Add remaining ingredients and cook for 10 minutes."
      ]
    }
  ]
}
""");
        sb.AppendLine("'appliance' must be one of: Oven, Stovetop, Oven + Stovetop.");
        sb.AppendLine("'equipment' must name the specific pan, pot, or tray needed (e.g. 'Large frying pan', 'Baking tray', '2L saucepan').");
        sb.AppendLine("'recipeSteps' must be a list of clear, numbered cooking steps — at least 4 steps, written in plain English.");

        if (strict)
        {
            sb.AppendLine("IMPORTANT: Your previous response failed validation.");
            sb.AppendLine("Ensure suggestions 1 and 2 have missingIngredients: []");
            sb.AppendLine("Ensure suggestion 3 has 1–3 items in missingIngredients.");
            sb.AppendLine("Return ONLY raw JSON.");
        }

        return sb.ToString();
    }

    private static string BuildUserMessage(
        IReadOnlyList<string> allIngredients,
        QuizAnswers quiz,
        List<string> recentMealNames,
        IReadOnlyList<string> exclusions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("AVAILABLE INGREDIENTS:");
        sb.AppendLine(string.Join(", ", allIngredients));
        sb.AppendLine();

        if (exclusions.Count > 0)
        {
            sb.AppendLine("EXCLUDED INGREDIENTS — STRICT RULE: Do NOT use any of these in any suggestion.");
            sb.AppendLine("This includes as a main ingredient, a sub-component, a sauce, or a garnish.");
            sb.AppendLine("Treat these as hard allergies — zero tolerance:");
            sb.AppendLine(string.Join(", ", exclusions));
            sb.AppendLine();
        }

        sb.AppendLine("TONIGHT'S QUIZ ANSWERS:");
        sb.AppendLine($"- Max cook time: {quiz.MaxMinutes} minutes");
        sb.AppendLine($"- Energy level: {quiz.EnergyLevel} — {EnergyDescription(quiz.EnergyLevel)}");
        sb.AppendLine($"- Health preference: {quiz.HealthPreference} — {HealthDescription(quiz.HealthPreference)}");
        sb.AppendLine($"- Craving: {quiz.CravingDirection} — {CravingDescription(quiz.CravingDirection)}");
        sb.AppendLine($"- Cuisine: {quiz.CuisinePreference}");
        sb.AppendLine();

        if (recentMealNames.Count > 0)
        {
            sb.AppendLine("RECENT MEAL HISTORY — avoid repeating these:");
            foreach (var meal in recentMealNames)
                sb.AppendLine($"- {meal}");
            sb.AppendLine();
        }

        sb.AppendLine("Provide exactly 3 dinner suggestions that strictly respect all quiz answers and exclusions above.");
        return sb.ToString();
    }

    private static string EnergyDescription(string level) => level switch
    {
        "low"  => "simple oven or stovetop, minimal chopping, minimal shopping required",
        "high" => "happy to properly cook tonight",
        _      => "something satisfying but not too complex"
    };

    private static string HealthDescription(string pref) => pref switch
    {
        "clean"   => "strictly low carb — protein and veg only, no grains, pasta, rice, or bread",
        "comfort" => "hearty and indulgent, satisfaction over nutrition",
        _         => "healthy but filling, balanced macros"
    };

    private static string CravingDescription(string dir) => dir switch
    {
        "comfort"  => "familiar classics only",
        "surprise" => "something bold or unexpected",
        _          => "something interesting but not too out there"
    };

    // ── HTTP call ─────────────────────────────────────────────────────────────

    private async Task<string> CallClaudeApiAsync(string userMessage, bool strict = false)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);

        var requestBody = new
        {
            model = ModelId,
            max_tokens = 1500,
            system = BuildSystemPrompt(strict),
            messages = new[] { new { role = "user", content = userMessage } }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(ApiEndpoint, content);
        }
        catch (HttpRequestException ex)
        {
            throw new SuggestionException("Could not reach the Claude API. Check your internet connection.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new SuggestionException($"Claude API returned {(int)response.StatusCode}: {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseJson);
        var textContent = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return textContent ?? throw new SuggestionException("Claude returned an empty response.");
    }

    // ── Response validation ───────────────────────────────────────────────────

    private static List<MealSuggestion>? ParseAndValidate(string responseText)
    {
        SuggestionResponse? parsed;
        try
        {
            var cleaned = responseText.Trim();
            if (cleaned.StartsWith("```")) cleaned = StripMarkdownFences(cleaned);
            parsed = JsonSerializer.Deserialize<SuggestionResponse>(cleaned, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (parsed?.Suggestions == null || parsed.Suggestions.Count != 3)
            return null;

        var s = parsed.Suggestions;

        // Suggestions 1 and 2 must use only available ingredients
        if (s[0].MissingIngredients.Count > 0) return null;
        if (s[1].MissingIngredients.Count > 0) return null;

        // Suggestion 3 must require 1–3 new ingredients
        if (s[2].MissingIngredients.Count is 0 or > 3) return null;

        return s;
    }

    private static string StripMarkdownFences(string text)
    {
        var lines = text.Split('\n');
        return string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
    }
}
