using System.Text.Json.Serialization;

namespace DinnerPicker.Models;

/// <summary>
/// Root object serialized to ~/.dinnerpicker/appdata.json.
/// Supports multiple user profiles; ActiveProfile gives the current user's data.
/// </summary>
public class AppData
{
    public string ActiveUserId { get; set; } = "";
    public Dictionary<string, UserProfile> Users { get; set; } = new();

    [JsonIgnore]
    public UserProfile ActiveProfile =>
        Users.TryGetValue(ActiveUserId, out var p) ? p : Users.Values.First();
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
