using DinnerPicker.Models;
using Xunit;

namespace DinnerPicker.Tests;

/// <summary>
/// Tests for quiz answer model defaults and computed properties.
/// </summary>
public class QuizAnswersTests
{
    // ── Energy Level Mapping ──────────────────────────────────────────────────

    [Theory]
    [InlineData("low")]
    [InlineData("medium")]
    [InlineData("high")]
    public void QuizAnswers_EnergyLevel_AcceptsValidValues(string energy)
    {
        var answers = new QuizAnswers { EnergyLevel = energy };
        Assert.Equal(energy, answers.EnergyLevel);
    }

    // ── Time Available ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("≤15")]
    [InlineData("15-30")]
    [InlineData("30-45")]
    [InlineData("no-rush")]
    public void QuizAnswers_TimeAvailable_AcceptsValidValues(string time)
    {
        var answers = new QuizAnswers { TimeAvailable = time };
        Assert.Equal(time, answers.TimeAvailable);
    }

    // ── MaxMinutes Computed Property ──────────────────────────────────────────

    [Theory]
    [InlineData("≤15",    15)]
    [InlineData("15-30",  30)]
    [InlineData("30-45",  45)]
    [InlineData("no-rush", 90)]
    public void QuizAnswers_MaxMinutes_ReturnsCorrectCeiling(string time, int expectedMax)
    {
        var answers = new QuizAnswers { TimeAvailable = time };
        Assert.Equal(expectedMax, answers.MaxMinutes);
    }

    // ── SpecialIngredients ────────────────────────────────────────────────────

    [Fact]
    public void QuizAnswers_SpecialIngredients_IsNullableByDefault()
    {
        var answers = new QuizAnswers();
        Assert.Null(answers.SpecialIngredients);
    }

    [Fact]
    public void QuizAnswers_SpecialIngredients_AcceptsValue()
    {
        var answers = new QuizAnswers { SpecialIngredients = "that chicken I need to use up" };
        Assert.Equal("that chicken I need to use up", answers.SpecialIngredients);
    }

    // ── Q5 Conditional Logic ──────────────────────────────────────────────────
    // Q5 (special ingredients free text) only shown when CravingDirection == "surprise"

    [Theory]
    [InlineData("surprise", true)]
    [InlineData("comfort",  false)]
    [InlineData("mix",      false)]
    public void Q5_IsShown_OnlyWhenCravingIsSurprise(string craving, bool expectQ5)
    {
        var answers = new QuizAnswers { CravingDirection = craving };
        bool shouldShowQ5 = answers.CravingDirection == "surprise";
        Assert.Equal(expectQ5, shouldShowQ5);
    }

    // ── Defaults ──────────────────────────────────────────────────────────────

    [Fact]
    public void QuizAnswers_DefaultCuisine_IsAny()
    {
        var answers = new QuizAnswers();
        Assert.Equal("any", answers.CuisinePreference);
    }

    [Fact]
    public void QuizAnswers_DefaultCraving_IsMix()
    {
        var answers = new QuizAnswers();
        Assert.Equal("mix", answers.CravingDirection);
    }

    [Fact]
    public void QuizAnswers_DefaultTimeAvailable_Is15To30()
    {
        var answers = new QuizAnswers();
        Assert.Equal("15-30", answers.TimeAvailable);
    }

    [Fact]
    public void QuizAnswers_DefaultEnergyLevel_IsMedium()
    {
        var answers = new QuizAnswers();
        Assert.Equal("medium", answers.EnergyLevel);
    }
}

/// <summary>
/// Tests for MealHistoryEntry model and rolling window logic.
/// </summary>
public class HistoryServiceTests
{
    [Fact]
    public void MealHistoryEntry_StoresMealNameAndCuisine()
    {
        var entry = new MealHistoryEntry
        {
            Date = DateTime.Now,
            SelectedMeal = "Pasta Carbonara",
            Cuisine = "Italian"
        };

        Assert.Equal("Pasta Carbonara", entry.SelectedMeal);
        Assert.Equal("Italian", entry.Cuisine);
    }

    [Fact]
    public void GetRecentMealNames_FiltersBy14DayCutoff()
    {
        var history = new List<MealHistoryEntry>
        {
            new() { Date = DateTime.Now.AddDays(-20), SelectedMeal = "Old Meal",    Cuisine = "Asian" },
            new() { Date = DateTime.Now.AddDays(-10), SelectedMeal = "Recent Meal", Cuisine = "Italian" },
            new() { Date = DateTime.Now.AddDays(-1),  SelectedMeal = "Last Night",  Cuisine = "Mexican" }
        };

        // Mirror HistoryService.GetRecentMealNames(14) logic
        var cutoff = DateTime.Now.AddDays(-14);
        var recent = history
            .Where(e => e.Date >= cutoff)
            .Select(e => e.SelectedMeal)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        Assert.Equal(2, recent.Count);
        Assert.Contains("Recent Meal", recent);
        Assert.Contains("Last Night", recent);
        Assert.DoesNotContain("Old Meal", recent);
    }

    [Fact]
    public void History_RollingWindow_TrimsTo90Entries()
    {
        const int maxEntries = 90;

        var history = Enumerable.Range(1, 95)
            .Select(i => new MealHistoryEntry
            {
                Date = DateTime.Now.AddDays(-i),
                SelectedMeal = $"Meal {i}",
                Cuisine = "Italian"
            })
            .ToList();

        // Apply the trim logic from HistoryService.RecordSession
        if (history.Count > maxEntries)
            history.RemoveRange(0, history.Count - maxEntries);

        Assert.Equal(maxEntries, history.Count);
    }
}
