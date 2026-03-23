namespace DinnerPicker.Services;

/// <summary>
/// Scoped (per-circuit) event bus so ActiveUserBar can ask SettingsButton to open a tab.
/// </summary>
public class SettingsNavigator
{
    public event Action<string>? OpenRequested;
    public void Open(string tab) => OpenRequested?.Invoke(tab);
}
