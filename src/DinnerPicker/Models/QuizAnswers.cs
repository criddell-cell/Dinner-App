namespace DinnerPicker.Models;

/// <summary>
/// Typed results from the adaptive evening quiz.
/// </summary>
public class QuizAnswers
{
    /// <summary>"≤15" | "15-30" | "30-45" | "no-rush"</summary>
    public string TimeAvailable { get; set; } = "15-30";

    /// <summary>"low" | "medium" | "high"</summary>
    public string EnergyLevel { get; set; } = "medium";

    /// <summary>"clean" | "balanced" | "comfort"</summary>
    public string HealthPreference { get; set; } = "balanced";

    /// <summary>"comfort" | "mix" | "surprise"</summary>
    public string CravingDirection { get; set; } = "mix";

    /// <summary>"mexican" | "asian" | "italian" | "american" | "any"</summary>
    public string CuisinePreference { get; set; } = "any";

    /// <summary>Returns the numeric minute ceiling for this time slot.</summary>
    public int MaxMinutes => TimeAvailable switch
    {
        "≤15"    => 15,
        "15-30"  => 30,
        "30-45"  => 45,
        "no-rush" => 90,
        _         => 30
    };
}
