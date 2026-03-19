using DinnerPicker.Models;

namespace DinnerPicker.Persistence;

public interface IDataStore
{
    /// <summary>
    /// Loads AppData from disk. Returns a default instance with IsFirstRun=true
    /// if no file exists yet.
    /// </summary>
    AppData Load();

    /// <summary>Serializes AppData to disk, creating directories if needed.</summary>
    void Save(AppData data);
}
