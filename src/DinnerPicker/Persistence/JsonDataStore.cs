using System.Text.Json;
using DinnerPicker.Models;

namespace DinnerPicker.Persistence;

/// <summary>
/// Persists AppData as a single JSON file at ~/.dinnerpicker/appdata.json.
/// Handles first-run (missing file), corrupted data, and migration from the
/// legacy single-user format.
/// </summary>
public class JsonDataStore : IDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
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
            return CreateFresh();

        try
        {
            var json = File.ReadAllText(_filePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Detect legacy single-user format (has PantryStaples at root, no Users key)
            if (root.TryGetProperty("PantryStaples", out _) && !root.TryGetProperty("Users", out _))
                return MigrateLegacy(root);

            return JsonSerializer.Deserialize<AppData>(json, SerializerOptions)
                   ?? CreateFresh();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            BackupCorruptedFile();
            return CreateFresh();
        }
    }

    public void Save(AppData data)
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(data, SerializerOptions);
        var tmp = _filePath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, _filePath, overwrite: true);
    }

    private static AppData CreateFresh()
    {
        var userId = Guid.NewGuid().ToString("N")[..8];
        return new AppData
        {
            ActiveUserId = userId,
            Users = new Dictionary<string, UserProfile>
            {
                [userId] = new UserProfile { Id = userId, Name = "Me", IsFirstRun = true }
            }
        };
    }

    private static AppData MigrateLegacy(JsonElement root)
    {
        var userId = "default";
        var profile = new UserProfile
        {
            Id = userId,
            Name = "Me",
            IsFirstRun = root.TryGetProperty("IsFirstRun", out var fr) && fr.GetBoolean(),
            PantryStaples = root.TryGetProperty("PantryStaples", out var ps)
                ? ps.Deserialize<List<string>>(SerializerOptions) ?? [] : [],
            FridgeContents = root.TryGetProperty("FridgeContents", out var fc)
                ? fc.Deserialize<List<string>>(SerializerOptions) ?? [] : [],
            MealHistory = root.TryGetProperty("MealHistory", out var mh)
                ? mh.Deserialize<List<MealHistoryEntry>>(SerializerOptions) ?? [] : [],
            Exclusions = root.TryGetProperty("Exclusions", out var ex)
                ? ex.Deserialize<List<string>>(SerializerOptions) ?? [] : []
        };

        return new AppData
        {
            ActiveUserId = userId,
            Users = new Dictionary<string, UserProfile> { [userId] = profile }
        };
    }

    private void BackupCorruptedFile()
    {
        try
        {
            var backup = _filePath + $".corrupted.{DateTime.Now:yyyyMMddHHmmss}";
            File.Move(_filePath, backup);
        }
        catch { }
    }
}
