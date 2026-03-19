using DinnerPicker.Models;
using DinnerPicker.Persistence;

namespace DinnerPicker.Services;

public class PantryService : IPantryService
{
    public static readonly List<string> DefaultStaples =
    [
        "olive oil", "garlic", "onion", "pasta", "rice", "eggs",
        "soy sauce", "canned tomatoes", "salt", "pepper", "butter",
        "flour", "vegetable broth", "hot sauce",
        // Spice rack
        "paprika", "cumin", "oregano", "chili powder", "cinnamon",
        "thyme", "garlic powder", "onion powder"
    ];

    private readonly IDataStore _store;
    private AppData _data;

    public PantryService(IDataStore store)
    {
        _store = store;
        _data = store.Load();

        // Seed defaults on first run (only if pantry is empty)
        if (_data.IsFirstRun && _data.PantryStaples.Count == 0)
            _data.PantryStaples = [.. DefaultStaples];
    }

    // ── First-run ─────────────────────────────────────────────────────────────

    public bool IsFirstRun => _data.IsFirstRun;

    public void CompleteSetup()
    {
        _data.IsFirstRun = false;
        _store.Save(_data);
    }

    // ── Pantry staples ────────────────────────────────────────────────────────

    public IReadOnlyList<string> GetPantryStaples() => _data.PantryStaples.AsReadOnly();

    public void SetPantryStaples(List<string> staples)
    {
        _data.PantryStaples = staples;
        _store.Save(_data);
    }

    public bool AddStaple(string ingredient)
    {
        var normalized = ingredient.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return false;
        if (_data.PantryStaples.Any(s => s.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            return false;

        _data.PantryStaples.Add(normalized);
        _store.Save(_data);
        return true;
    }

    public bool RemoveStaple(string ingredient)
    {
        var match = _data.PantryStaples
            .FirstOrDefault(s => s.Equals(ingredient.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match == null) return false;

        _data.PantryStaples.Remove(match);
        _store.Save(_data);
        return true;
    }

    public void ResetToDefaults()
    {
        _data.PantryStaples = [.. DefaultStaples];
        _store.Save(_data);
    }

    // ── Fridge contents ───────────────────────────────────────────────────────

    public bool AddFridgeItem(string ingredient)
    {
        var normalized = ingredient.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return false;
        if (_data.FridgeContents.Any(s => s.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            return false;

        _data.FridgeContents.Add(normalized);
        _store.Save(_data);
        return true;
    }

    public bool RemoveFridgeItem(string ingredient)
    {
        var match = _data.FridgeContents
            .FirstOrDefault(s => s.Equals(ingredient.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match == null) return false;

        _data.FridgeContents.Remove(match);
        _store.Save(_data);
        return true;
    }

    public void ClearFridgeContents()
    {
        _data.FridgeContents.Clear();
        _store.Save(_data);
    }

    public IReadOnlyList<string> GetFridgeContents() => _data.FridgeContents.AsReadOnly();

    public IReadOnlyList<string> GetAllIngredients() =>
        _data.PantryStaples
             .Concat(_data.FridgeContents)
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .ToList()
             .AsReadOnly();

    // ── Exclusions ────────────────────────────────────────────────────────────

    public IReadOnlyList<string> GetExclusions() => _data.Exclusions.AsReadOnly();

    public bool AddExclusion(string ingredient)
    {
        var normalized = ingredient.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return false;
        if (_data.Exclusions.Any(s => s.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            return false;

        _data.Exclusions.Add(normalized);
        _store.Save(_data);
        return true;
    }

    public bool RemoveExclusion(string ingredient)
    {
        var match = _data.Exclusions
            .FirstOrDefault(s => s.Equals(ingredient.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match == null) return false;

        _data.Exclusions.Remove(match);
        _store.Save(_data);
        return true;
    }

    // Internal: share the same AppData instance with HistoryService
    internal AppData GetAppData() => _data;
}
