namespace DinnerPicker.Models;

public class UserProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsFirstRun { get; set; } = true;
    public List<string> PantryStaples { get; set; } = [];
    public List<string> FridgeContents { get; set; } = [];
    public List<MealHistoryEntry> MealHistory { get; set; } = [];
    public List<string> Exclusions { get; set; } = [];
}
