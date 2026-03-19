using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace DinnerPicker.E2E;

// ─────────────────────────────────────────────────────────────────────────────
// PANTRY PAGE — EXPECTED BEHAVIOUR SPECIFICATION
// ─────────────────────────────────────────────────────────────────────────────
//
// Page: /pantry
//
// ELEMENTS
// ┌─────────────────────────┬──────────────────────────────────────────────────┐
// │ Element                 │ Selector                                          │
// ├─────────────────────────┼──────────────────────────────────────────────────┤
// │ Pantry grid             │ .pantry-grid                                      │
// │ Individual item chips   │ .pantry-item                                      │
// │ Remove (×) buttons      │ .btn-remove                                       │
// │ Add input               │ input.input[placeholder*="Add"]                   │
// │ Add button              │ button:has-text("Add")                            │
// │ Reset to defaults button│ button:has-text("Reset to defaults")              │
// │ Item count label        │ .action-row .muted.small                          │
// │ Success alert           │ .alert-success                                    │
// │ Error alert             │ .alert-error                                      │
// └─────────────────────────┴──────────────────────────────────────────────────┘
//
// BUTTON BEHAVIOURS
//
// [Add button]
//   GIVEN  the input contains a new ingredient name
//   WHEN   the user clicks Add (or presses Enter)
//   THEN   the ingredient chip appears in the pantry grid
//   AND    the input is cleared
//   AND    a green success alert is shown
//   AND    the item count increments by 1
//
//   GIVEN  the input is empty
//   WHEN   the user clicks Add
//   THEN   nothing changes — no chip added, no alert, no count change
//
//   GIVEN  the input contains a name already in the pantry (any casing)
//   WHEN   the user clicks Add
//   THEN   no new chip is added
//   AND    a red error alert is shown ("already in your pantry")
//   AND    the item count is unchanged
//
// [Remove (×) button]
//   GIVEN  a pantry item chip is visible
//   WHEN   the user clicks its × button
//   THEN   the chip is removed from the grid
//   AND    a green success alert is shown
//   AND    the item count decrements by 1
//
// [Reset to defaults button]
//   GIVEN  the pantry has been modified (items added or removed)
//   WHEN   the user clicks Reset to defaults
//   THEN   the pantry grid shows exactly the 14 default staples
//   AND    any custom additions are gone
//   AND    a green success alert is shown
//   AND    the item count is 14
//
// ─────────────────────────────────────────────────────────────────────────────

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class PantryPageTests : PageTest
{
    // Base URL — override via DINNER_PICKER_BASE_URL environment variable for CI
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("DINNER_PICKER_BASE_URL") ?? "http://localhost:5000";

    private const string AddInputSelector    = "input.input[placeholder*='ingredient']";
    private const string AddButtonSelector   = "button:has-text('Add')";
    private const string ResetButtonSelector = "button:has-text('Reset to defaults')";
    private const string ItemSelector        = ".pantry-item";
    private const string CountSelector       = ".action-row .muted.small";
    private const string SuccessSelector     = ".alert-success";
    private const string ErrorSelector       = ".alert-error";

    // ── Setup ─────────────────────────────────────────────────────────────────

    [SetUp]
    public async Task NavigateAndReset()
    {
        await Page.GotoAsync($"{BaseUrl}/pantry");

        // Wait for Blazor Server SignalR circuit to establish and render the grid
        await Page.WaitForSelectorAsync(".pantry-grid",
            new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

        // Reset pantry to defaults before every test to ensure a clean, known state
        await Page.ClickAsync(ResetButtonSelector);
        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });
    }

    // ── Page Load ─────────────────────────────────────────────────────────────

    [Test]
    [Description("Page loads and renders the default pantry staples in the grid")]
    public async Task PageLoad_ShowsDefaultStaples()
    {
        var items = await Page.QuerySelectorAllAsync(ItemSelector);
        Assert.That(items, Has.Count.EqualTo(14),
            "Should show the 14 default pantry staples on first load");
    }

    [Test]
    [Description("Item count label reflects the current number of staples")]
    public async Task PageLoad_ItemCountLabel_MatchesGridCount()
    {
        var items     = await Page.QuerySelectorAllAsync(ItemSelector);
        var countText = await Page.TextContentAsync(CountSelector);

        Assert.That(countText, Does.Contain(items.Count.ToString()),
            "Count label should match the number of chips in the grid");
    }

    // ── Add Button ────────────────────────────────────────────────────────────

    [Test]
    [Description("Typing a new ingredient and clicking Add appends it to the grid")]
    public async Task AddButton_Click_AddsItemToGrid()
    {
        await Page.FillAsync(AddInputSelector, "tahini");
        await Page.ClickAsync(AddButtonSelector);

        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        var itemTexts = await GetAllItemTextsAsync();
        Assert.That(itemTexts, Contains.Item("tahini"),
            "Newly added item should appear in the pantry grid");
    }

    [Test]
    [Description("Pressing Enter in the input adds the item — same as clicking Add")]
    public async Task AddInput_PressEnter_AddsItemToGrid()
    {
        await Page.FillAsync(AddInputSelector, "miso paste");
        await Page.PressAsync(AddInputSelector, "Enter");

        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        var itemTexts = await GetAllItemTextsAsync();
        Assert.That(itemTexts, Contains.Item("miso paste"),
            "Pressing Enter should add the item just like clicking Add");
    }

    [Test]
    [Description("Clicking Add clears the input field")]
    public async Task AddButton_Click_ClearsInput()
    {
        await Page.FillAsync(AddInputSelector, "tahini");
        await Page.ClickAsync(AddButtonSelector);

        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        var inputValue = await Page.InputValueAsync(AddInputSelector);
        Assert.That(inputValue, Is.Empty, "Input field should be empty after a successful add");
    }

    [Test]
    [Description("Clicking Add shows a green success alert")]
    public async Task AddButton_Click_ShowsSuccessAlert()
    {
        await Page.FillAsync(AddInputSelector, "tahini");
        await Page.ClickAsync(AddButtonSelector);

        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        var alertText = await Page.TextContentAsync(SuccessSelector);
        Assert.That(alertText, Does.Contain("tahini"),
            "Success alert should mention the ingredient that was added");
    }

    [Test]
    [Description("Item count increments by 1 after a successful add")]
    public async Task AddButton_Click_IncrementsItemCount()
    {
        var countBefore = await GetItemCountAsync();

        await Page.FillAsync(AddInputSelector, "tahini");
        await Page.ClickAsync(AddButtonSelector);
        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        var countAfter = await GetItemCountAsync();
        Assert.That(countAfter, Is.EqualTo(countBefore + 1),
            "Item count should increase by 1 after adding a new staple");
    }

    [Test]
    [Description("Clicking Add with an empty input does nothing")]
    public async Task AddButton_EmptyInput_DoesNotAddItem()
    {
        var countBefore = await GetItemCountAsync();

        // Ensure input is empty and click Add
        await Page.FillAsync(AddInputSelector, "");
        await Page.ClickAsync(AddButtonSelector);

        // Brief pause — no alert should appear
        await Page.WaitForTimeoutAsync(500);

        var countAfter  = await GetItemCountAsync();
        var hasAlert    = await Page.IsVisibleAsync($"{SuccessSelector}, {ErrorSelector}");

        Assert.That(countAfter, Is.EqualTo(countBefore), "Count should not change on empty add");
        Assert.That(hasAlert,   Is.False,                "No alert should appear for empty input");
    }

    [Test]
    [Description("Adding a duplicate ingredient shows an error and does not add to the list")]
    public async Task AddButton_DuplicateItem_ShowsErrorAlert()
    {
        var countBefore = await GetItemCountAsync();

        // "garlic" is in the default pantry
        await Page.FillAsync(AddInputSelector, "garlic");
        await Page.ClickAsync(AddButtonSelector);

        await Page.WaitForSelectorAsync(ErrorSelector,
            new() { State = WaitForSelectorState.Visible });

        var countAfter = await GetItemCountAsync();
        var alertText  = await Page.TextContentAsync(ErrorSelector);

        Assert.That(countAfter, Is.EqualTo(countBefore), "Count should not change for a duplicate");
        Assert.That(alertText,  Does.Contain("already"), "Error should mention the item already exists");
    }

    [Test]
    [Description("Duplicate check is case-insensitive (GARLIC = garlic)")]
    public async Task AddButton_DuplicateItem_CaseInsensitive_ShowsError()
    {
        await Page.FillAsync(AddInputSelector, "GARLIC");
        await Page.ClickAsync(AddButtonSelector);

        await Page.WaitForSelectorAsync(ErrorSelector,
            new() { State = WaitForSelectorState.Visible });

        Assert.That(await Page.IsVisibleAsync(ErrorSelector), Is.True,
            "Uppercase duplicate should still be rejected");
    }

    // ── Remove (×) Button ─────────────────────────────────────────────────────

    [Test]
    [Description("Clicking × removes the item from the pantry grid")]
    public async Task RemoveButton_Click_RemovesItemFromGrid()
    {
        // Get the name of the first item before removal
        var firstItemText = await Page.TextContentAsync($"{ItemSelector} span");
        var itemName = firstItemText?.Trim() ?? "";

        await Page.ClickAsync($"{ItemSelector} .btn-remove");

        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        var itemTexts = await GetAllItemTextsAsync();
        Assert.That(itemTexts, Does.Not.Contain(itemName),
            "Removed item should no longer appear in the grid");
    }

    [Test]
    [Description("Clicking × shows a success alert confirming removal")]
    public async Task RemoveButton_Click_ShowsSuccessAlert()
    {
        await Page.ClickAsync($"{ItemSelector} .btn-remove");

        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        Assert.That(await Page.IsVisibleAsync(SuccessSelector), Is.True,
            "A success alert should appear after removal");
    }

    [Test]
    [Description("Item count decrements by 1 after removal")]
    public async Task RemoveButton_Click_DecrementsItemCount()
    {
        var countBefore = await GetItemCountAsync();

        await Page.ClickAsync($"{ItemSelector} .btn-remove");
        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        var countAfter = await GetItemCountAsync();
        Assert.That(countAfter, Is.EqualTo(countBefore - 1),
            "Item count should decrease by 1 after removing a staple");
    }

    // ── Reset to Defaults Button ──────────────────────────────────────────────

    [Test]
    [Description("Reset to defaults restores exactly 14 default items")]
    public async Task ResetButton_Click_RestoresDefaultItemCount()
    {
        // Add a custom item first so the state is different from defaults
        await Page.FillAsync(AddInputSelector, "tahini");
        await Page.ClickAsync(AddButtonSelector);
        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        // Now reset
        await Page.ClickAsync(ResetButtonSelector);
        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        var countAfter = await GetItemCountAsync();
        Assert.That(countAfter, Is.EqualTo(14),
            "After reset, pantry should have exactly 14 default staples");
    }

    [Test]
    [Description("Reset removes any custom items that were added")]
    public async Task ResetButton_Click_RemovesCustomAdditions()
    {
        await Page.FillAsync(AddInputSelector, "tahini");
        await Page.ClickAsync(AddButtonSelector);
        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        await Page.ClickAsync(ResetButtonSelector);
        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        var itemTexts = await GetAllItemTextsAsync();
        Assert.That(itemTexts, Does.Not.Contain("tahini"),
            "Custom item should be gone after reset");
    }

    [Test]
    [Description("Reset shows a success alert")]
    public async Task ResetButton_Click_ShowsSuccessAlert()
    {
        await Page.ClickAsync(ResetButtonSelector);

        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        Assert.That(await Page.IsVisibleAsync(SuccessSelector), Is.True,
            "Success alert should appear after reset");
    }

    [Test]
    [Description("After reset the standard default staples are all present")]
    public async Task ResetButton_Click_ContainsExpectedDefaults()
    {
        await Page.ClickAsync(ResetButtonSelector);
        await Page.WaitForSelectorAsync(SuccessSelector,
            new() { State = WaitForSelectorState.Visible });

        var itemTexts = await GetAllItemTextsAsync();

        string[] expectedDefaults = ["olive oil", "garlic", "onion", "pasta", "rice", "eggs",
                                      "soy sauce", "canned tomatoes", "salt", "pepper",
                                      "butter", "flour", "vegetable broth", "hot sauce"];

        foreach (var expected in expectedDefaults)
            Assert.That(itemTexts, Contains.Item(expected),
                $"Default staple '{expected}' should be present after reset");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns the text of every pantry chip currently in the grid.</summary>
    private async Task<List<string>> GetAllItemTextsAsync()
    {
        var spans = await Page.QuerySelectorAllAsync($"{ItemSelector} span");
        var texts = new List<string>();
        foreach (var span in spans)
        {
            var text = await span.TextContentAsync();
            if (text != null) texts.Add(text.Trim());
        }
        return texts;
    }

    /// <summary>Reads the current item count from the grid (chip count, not the label).</summary>
    private async Task<int> GetItemCountAsync()
    {
        var items = await Page.QuerySelectorAllAsync(ItemSelector);
        return items.Count;
    }
}
