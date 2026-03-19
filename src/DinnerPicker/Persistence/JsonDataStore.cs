using System.Text.Json;
using DinnerPicker.Models;

namespace DinnerPicker.Persistence;

/// <summary>
/// Persists AppData as a single JSON file at ~/.dinnerpicker/appdata.json.
/// Handles first-run (missing file) and corrupted data gracefully.
/// </summary>
public class JsonDataStore : IDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public JsonDataStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dinnerpicker");
        _filePath = Path.Combine(dir, "appdata.json");
    }

    public AppData Load()
    {
        if (!File.Exists(_filePath))
            return new AppData { IsFirstRun = true };

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppData>(json, SerializerOptions)
                   ?? new AppData { IsFirstRun = true };
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Corrupted or unreadable file — start fresh rather than crash.
            // Back up the bad file so the user can inspect it if needed.
            BackupCorruptedFile();
            return new AppData { IsFirstRun = true };
        }
    }

    public void Save(AppData data)
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(data, SerializerOptions);
        // Write to a temp file first, then replace atomically to avoid
        // partial writes corrupting the data file on power loss.
        var tmp = _filePath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, _filePath, overwrite: true);
    }

    private void BackupCorruptedFile()
    {
        try
        {
            var backup = _filePath + $".corrupted.{DateTime.Now:yyyyMMddHHmmss}";
            File.Move(_filePath, backup);
        }
        catch
        {
            // Best effort — ignore if backup also fails
        }
    }
}
