namespace DinnerPicker.Services;

public interface IPantryService
{
    // ── First-run state ───────────────────────────────────────────────────────
    bool IsFirstRun { get; }

    /// <summary>Marks setup as complete and persists the flag.</summary>
    void CompleteSetup();

    // ── Pantry staples ────────────────────────────────────────────────────────
    IReadOnlyList<string> GetPantryStaples();
    void SetPantryStaples(List<string> staples);
    bool AddStaple(string ingredient);
    bool RemoveStaple(string ingredient);
    void ResetToDefaults();

    // ── Fridge contents (persisted, cleared after session) ───────────────────

    /// <summary>
    /// Adds a single fresh ingredient to the fridge list and persists it.
    /// Returns false if it already exists.
    /// </summary>
    bool AddFridgeItem(string ingredient);

    /// <summary>Removes a fridge item and persists. Returns false if not found.</summary>
    bool RemoveFridgeItem(string ingredient);

    /// <summary>
    /// Clears all fridge contents and persists.
    /// Called when a session completes (meal selected).
    /// </summary>
    void ClearFridgeContents();

    IReadOnlyList<string> GetFridgeContents();

    /// <summary>Returns combined pantry staples + fridge contents (deduplicated).</summary>
    IReadOnlyList<string> GetAllIngredients();

    // ── Exclusions (allergies / dislikes) ────────────────────────────────────
    IReadOnlyList<string> GetExclusions();
    bool AddExclusion(string ingredient);
    bool RemoveExclusion(string ingredient);
}
