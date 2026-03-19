namespace DinnerPicker.Models;

/// <summary>
/// Root object serialized to ~/.dinnerpicker/appdata.json.
/// </summary>
public class AppData
{
    public bool IsFirstRun { get; set; } = true;
    public List<string> PantryStaples { get; set; } = [];

    /// <summary>
    /// Persisted between sessions so the user doesn't re-enter everything.
    /// Cleared when a session is completed (meal selected).
    /// </summary>
    public List<string> FridgeContents { get; set; } = [];

    public List<MealHistoryEntry> MealHistory { get; set; } = [];

    /// <summary>Ingredients the user wants excluded from all suggestions (allergies / dislikes).</summary>
    public List<string> Exclusions { get; set; } = [];
}

/// <summary>
/// Records the single meal the user chose at the end of a session.
/// </summary>
public class MealHistoryEntry
{
    public DateTime Date { get; set; }
    public string SelectedMeal { get; set; } = "";
    public string Cuisine { get; set; } = "";
    public bool IsFavourite { get; set; } = false;

    /// <summary>Full meal data for the history detail view. Null for entries saved before this field was added.</summary>
    public MealSuggestion? FullMeal { get; set; }

    /// <summary>Filename (not full path) of an uploaded meal photo stored in ~/.dinnerpicker/images/.</summary>
    public string? PhotoFileName { get; set; }
}
