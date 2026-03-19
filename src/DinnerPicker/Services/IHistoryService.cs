using DinnerPicker.Models;

namespace DinnerPicker.Services;

public interface IHistoryService
{
    /// <summary>
    /// Returns the names of meals selected in the last N days.
    /// Used to inject variety context into the Claude prompt.
    /// </summary>
    List<string> GetRecentMealNames(int days = 14);

    /// <summary>
    /// Records the meal the user chose at the end of a session.
    /// </summary>
    void RecordSession(MealSuggestion selected);

    /// <summary>Returns all history entries (newest first) for the history screen.</summary>
    IReadOnlyList<MealHistoryEntry> GetAllHistory();

    /// <summary>Returns only favourited entries (newest first).</summary>
    IReadOnlyList<MealHistoryEntry> GetFavourites();

    /// <summary>Toggles the favourite state of an entry and persists.</summary>
    void ToggleFavourite(MealHistoryEntry entry);

    /// <summary>Saves an uploaded photo for an entry, replacing any existing one. Returns the URL path to serve it.</summary>
    Task<string> SavePhotoAsync(MealHistoryEntry entry, Stream stream, string contentType);

    /// <summary>Deletes the photo for an entry and persists.</summary>
    void DeletePhoto(MealHistoryEntry entry);
}
