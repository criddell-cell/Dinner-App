using DinnerPicker.Models;
using DinnerPicker.Persistence;

namespace DinnerPicker.Services;

public class HistoryService : IHistoryService
{
    private readonly IDataStore _store;
    private readonly AppData _data;

    public HistoryService(IDataStore store, PantryService pantryService)
    {
        _store = store;
        // Share the same AppData already loaded by PantryService
        _data = pantryService.GetAppData();
    }

    public List<string> GetRecentMealNames(int days = 14)
    {
        var cutoff = DateTime.Now.AddDays(-days);
        return _data.MealHistory
            .Where(e => e.Date >= cutoff)
            .Select(e => e.SelectedMeal)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();
    }

    public void RecordSession(MealSuggestion selected)
    {
        _data.MealHistory.Add(new MealHistoryEntry
        {
            Date = DateTime.Now,
            SelectedMeal = selected.Name,
            Cuisine = selected.Cuisine,
            FullMeal = selected
        });

        // Keep at most 90 entries (~3 months of daily use)
        if (_data.MealHistory.Count > 90)
            _data.MealHistory.RemoveRange(0, _data.MealHistory.Count - 90);

        _store.Save(_data);
    }

    public IReadOnlyList<MealHistoryEntry> GetAllHistory() =>
        _data.MealHistory.AsEnumerable().Reverse().ToList().AsReadOnly();

    public IReadOnlyList<MealHistoryEntry> GetFavourites() =>
        _data.MealHistory.Where(e => e.IsFavourite).Reverse().ToList().AsReadOnly();

    public void ToggleFavourite(MealHistoryEntry entry)
    {
        var match = _data.MealHistory.FirstOrDefault(e =>
            e.Date == entry.Date && e.SelectedMeal == entry.SelectedMeal);
        if (match == null) return;
        match.IsFavourite = !match.IsFavourite;
        _store.Save(_data);
    }

    public async Task<string> SavePhotoAsync(MealHistoryEntry entry, Stream stream, string contentType)
    {
        var match = _data.MealHistory.FirstOrDefault(e =>
            e.Date == entry.Date && e.SelectedMeal == entry.SelectedMeal);
        if (match == null) return string.Empty;

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dinnerpicker", "images");
        Directory.CreateDirectory(dir);

        // Delete old photo file if replacing
        if (!string.IsNullOrEmpty(match.PhotoFileName))
        {
            var old = Path.Combine(dir, match.PhotoFileName);
            if (File.Exists(old)) File.Delete(old);
        }

        var ext = contentType switch
        {
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(dir, fileName);

        using var fs = File.Create(filePath);
        await stream.CopyToAsync(fs);

        match.PhotoFileName = fileName;
        entry.PhotoFileName = fileName;
        _store.Save(_data);

        return $"/meal-photos/{fileName}";
    }

    public void DeletePhoto(MealHistoryEntry entry)
    {
        var match = _data.MealHistory.FirstOrDefault(e =>
            e.Date == entry.Date && e.SelectedMeal == entry.SelectedMeal);
        if (match == null || string.IsNullOrEmpty(match.PhotoFileName)) return;

        var filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dinnerpicker", "images", match.PhotoFileName);
        if (File.Exists(filePath)) File.Delete(filePath);

        match.PhotoFileName = null;
        entry.PhotoFileName = null;
        _store.Save(_data);
    }
}
