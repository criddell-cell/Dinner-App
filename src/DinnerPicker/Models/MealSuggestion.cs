using System.Text.Json.Serialization;

namespace DinnerPicker.Models;

/// <summary>
/// A single dinner suggestion as returned by the Claude API.
/// Field names match exactly what the prompt requests so deserialization
/// requires no custom mapping.
/// </summary>
public class MealSuggestion
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cuisine")]
    public string Cuisine { get; set; } = string.Empty;

    [JsonPropertyName("estimatedMinutes")]
    public int EstimatedMinutes { get; set; }

    [JsonPropertyName("healthNotes")]
    public string HealthNotes { get; set; } = string.Empty;

    /// <summary>e.g. "Oven", "Stovetop", "Oven + Stovetop"</summary>
    [JsonPropertyName("appliance")]
    public string Appliance { get; set; } = string.Empty;

    /// <summary>e.g. "Large frying pan", "Baking tray + saucepan"</summary>
    [JsonPropertyName("equipment")]
    public string Equipment { get; set; } = string.Empty;

    /// <summary>All ingredients used (from pantry + fridge).</summary>
    [JsonPropertyName("ingredients")]
    public List<string> Ingredients { get; set; } = [];

    /// <summary>
    /// Ingredients the user needs to buy.
    /// Empty for suggestions 1 and 2; 1–3 items for suggestion 3.
    /// </summary>
    [JsonPropertyName("missingIngredients")]
    public List<string> MissingIngredients { get; set; } = [];

    /// <summary>Brief 1–2 sentence description shown on the suggestion card.</summary>
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = string.Empty;

    /// <summary>Numbered recipe steps shown on the complete screen.</summary>
    [JsonPropertyName("recipeSteps")]
    public List<string> RecipeSteps { get; set; } = [];

    /// <summary>Estimated average online rating for this recipe (3.5–5.0). Only recipes ≥3.5 should be suggested.</summary>
    [JsonPropertyName("rating")]
    public double Rating { get; set; }
}

/// <summary>Wrapper matching the top-level JSON structure Claude returns.</summary>
public class SuggestionResponse
{
    [JsonPropertyName("suggestions")]
    public List<MealSuggestion> Suggestions { get; set; } = [];
}
