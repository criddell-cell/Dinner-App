using DinnerPicker.Models;

namespace DinnerPicker.Services;

public interface ISuggestionService
{
    /// <summary>
    /// Calls the Claude API and returns exactly 3 dinner suggestions.
    /// </summary>
    /// <exception cref="SuggestionException">
    /// Thrown when the API is unavailable, returns an error, or returns
    /// a malformed/invalid response that can't be corrected with a retry.
    /// </exception>
    Task<List<MealSuggestion>> GetSuggestionsAsync(
        IReadOnlyList<string> allIngredients,
        QuizAnswers quiz,
        List<string> recentMealNames,
        IReadOnlyList<string> exclusions);
}

/// <summary>Raised when the suggestion engine cannot produce results.</summary>
public class SuggestionException : Exception
{
    public SuggestionException(string message, Exception? inner = null)
        : base(message, inner) { }
}
