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
    private readonly AppData _data;

    public PantryService(IDataStore store)
    {
        _store = store;
        _data = store.Load();

        // Seed defaults for the active profile on first run
        var profile = _data.ActiveProfile;
        if (profile.IsFirstRun && profile.PantryStaples.Count == 0)
            profile.PantryStaples = [.. DefaultStaples];
    }

    // ── First-run ─────────────────────────────────────────────────────────────

    public bool IsFirstRun => _data.ActiveProfile.IsFirstRun;

    public void CompleteSetup()
    {
        _data.ActiveProfile.IsFirstRun = false;
        _store.Save(_data);
    }

    // ── Pantry staples ────────────────────────────────────────────────────────

    public IReadOnlyList<string> GetPantryStaples() => _data.ActiveProfile.PantryStaples.AsReadOnly();

    public void SetPantryStaples(List<string> staples)
    {
        _data.ActiveProfile.PantryStaples = staples;
        _store.Save(_data);
    }

    public bool AddStaple(string ingredient)
    {
        var normalized = ingredient.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return false;
        if (_data.ActiveProfile.PantryStaples.Any(s => s.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            return false;

        _data.ActiveProfile.PantryStaples.Add(normalized);
        _store.Save(_data);
        return true;
    }

    public bool RemoveStaple(string ingredient)
    {
        var match = _data.ActiveProfile.PantryStaples
            .FirstOrDefault(s => s.Equals(ingredient.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match == null) return false;

        _data.ActiveProfile.PantryStaples.Remove(match);
        _store.Save(_data);
        return true;
    }

    public void ResetToDefaults()
    {
        _data.ActiveProfile.PantryStaples = [.. DefaultStaples];
        _store.Save(_data);
    }

    // ── Fridge contents ───────────────────────────────────────────────────────

    public bool AddFridgeItem(string ingredient)
    {
        var normalized = ingredient.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return false;
        if (_data.ActiveProfile.FridgeContents.Any(s => s.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            return false;

        _data.ActiveProfile.FridgeContents.Add(normalized);
        _store.Save(_data);
        return true;
    }

    public bool RemoveFridgeItem(string ingredient)
    {
        var match = _data.ActiveProfile.FridgeContents
            .FirstOrDefault(s => s.Equals(ingredient.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match == null) return false;

        _data.ActiveProfile.FridgeContents.Remove(match);
        _store.Save(_data);
        return true;
    }

    public void ClearFridgeContents()
    {
        _data.ActiveProfile.FridgeContents.Clear();
        _store.Save(_data);
    }

    public IReadOnlyList<string> GetFridgeContents() => _data.ActiveProfile.FridgeContents.AsReadOnly();

    public IReadOnlyList<string> GetAllIngredients() =>
        _data.ActiveProfile.PantryStaples
             .Concat(_data.ActiveProfile.FridgeContents)
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .ToList()
             .AsReadOnly();

    // ── Exclusions ────────────────────────────────────────────────────────────

    public IReadOnlyList<string> GetExclusions() => _data.ActiveProfile.Exclusions.AsReadOnly();

    public bool AddExclusion(string ingredient)
    {
        var normalized = ingredient.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return false;
        if (_data.ActiveProfile.Exclusions.Any(s => s.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            return false;

        _data.ActiveProfile.Exclusions.Add(normalized);
        _store.Save(_data);
        return true;
    }

    public bool RemoveExclusion(string ingredient)
    {
        var match = _data.ActiveProfile.Exclusions
            .FirstOrDefault(s => s.Equals(ingredient.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match == null) return false;

        _data.ActiveProfile.Exclusions.Remove(match);
        _store.Save(_data);
        return true;
    }

    // Internal: share the same AppData instance with other services
    internal AppData GetAppData() => _data;
}
