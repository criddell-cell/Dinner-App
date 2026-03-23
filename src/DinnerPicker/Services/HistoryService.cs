using DinnerPicker.Models;
using DinnerPicker.Persistence;

namespace DinnerPicker.Services;

public class HistoryService : IHistoryService
{
    private readonly IDataStore _store;
    private readonly AppData _appData;

    public HistoryService(IDataStore store, PantryService pantryService)
    {
        _store = store;
        _appData = pantryService.GetAppData();
    }

    private UserProfile Profile => _appData.ActiveProfile;

    public List<string> GetRecentMealNames(int days = 14)
    {
        var cutoff = DateTime.Now.AddDays(-days);
        return Profile.MealHistory
            .Where(e => e.Date >= cutoff)
            .Select(e => e.SelectedMeal)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();
    }

    public void RecordSession(MealSuggestion selected)
    {
        Profile.MealHistory.Add(new MealHistoryEntry
        {
            Date = DateTime.Now,
            SelectedMeal = selected.Name,
            Cuisine = selected.Cuisine,
            FullMeal = selected
        });

        if (Profile.MealHistory.Count > 90)
            Profile.MealHistory.RemoveRange(0, Profile.MealHistory.Count - 90);

        _store.Save(_appData);
    }

    public IReadOnlyList<MealHistoryEntry> GetAllHistory() =>
        Profile.MealHistory.AsEnumerable().Reverse().ToList().AsReadOnly();

    public IReadOnlyList<MealHistoryEntry> GetFavourites() =>
        Profile.MealHistory.Where(e => e.IsFavourite).Reverse().ToList().AsReadOnly();

    public void ToggleFavourite(MealHistoryEntry entry)
    {
        var match = Profile.MealHistory.FirstOrDefault(e =>
            e.Date == entry.Date && e.SelectedMeal == entry.SelectedMeal);
        if (match == null) return;
        match.IsFavourite = !match.IsFavourite;
        _store.Save(_appData);
    }

    public async Task<string> SavePhotoAsync(MealHistoryEntry entry, Stream stream, string contentType)
    {
        var match = Profile.MealHistory.FirstOrDefault(e =>
            e.Date == entry.Date && e.SelectedMeal == entry.SelectedMeal);
        if (match == null) return string.Empty;

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dinnerpicker", "images");
        Directory.CreateDirectory(dir);

        if (!string.IsNullOrEmpty(match.PhotoFileName))
        {
            var old = Path.Combine(dir, match.PhotoFileName);
            if (File.Exists(old)) File.Delete(old);
        }

        var ext = contentType switch
        {
            "image/png"  => ".png",
            "image/gif"  => ".gif",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(dir, fileName);

        using var fs = File.Create(filePath);
        await stream.CopyToAsync(fs);

        match.PhotoFileName = fileName;
        entry.PhotoFileName = fileName;
        _store.Save(_appData);

        return $"/meal-photos/{fileName}";
    }

    public void DeletePhoto(MealHistoryEntry entry)
    {
        var match = Profile.MealHistory.FirstOrDefault(e =>
            e.Date == entry.Date && e.SelectedMeal == entry.SelectedMeal);
        if (match == null || string.IsNullOrEmpty(match.PhotoFileName)) return;

        var filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dinnerpicker", "images", match.PhotoFileName);
        if (File.Exists(filePath)) File.Delete(filePath);

        match.PhotoFileName = null;
        entry.PhotoFileName = null;
        _store.Save(_appData);
    }
}
