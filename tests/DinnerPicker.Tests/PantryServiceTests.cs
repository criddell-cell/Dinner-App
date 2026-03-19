using DinnerPicker.Models;
using DinnerPicker.Persistence;
using DinnerPicker.Services;
using Moq;
using Xunit;

namespace DinnerPicker.Tests;

public class PantryServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (PantryService service, Mock<IDataStore> storeMock) CreateService(
        AppData? initialData = null)
    {
        var mock = new Mock<IDataStore>();
        mock.Setup(s => s.Load()).Returns(initialData ?? new AppData { IsFirstRun = true });

        var service = new PantryService(mock.Object);
        return (service, mock);
    }

    // ── Initialization ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SeedsDefaults_OnFirstRun()
    {
        var (service, _) = CreateService(new AppData { IsFirstRun = true, PantryStaples = [] });

        Assert.Equal(PantryService.DefaultStaples.Count, service.GetPantryStaples().Count);
    }

    [Fact]
    public void Constructor_DoesNotOverwriteExisting_OnFirstRun()
    {
        var existing = new AppData
        {
            IsFirstRun = true,
            PantryStaples = ["custom item"]
        };
        var (service, _) = CreateService(existing);

        // Existing non-empty pantry should not be replaced with defaults
        Assert.Contains("custom item", service.GetPantryStaples());
    }

    // ── Add Staple ────────────────────────────────────────────────────────────

    [Fact]
    public void AddStaple_ReturnsTrue_WhenNew()
    {
        var (service, _) = CreateService();
        var result = service.AddStaple("tahini");
        Assert.True(result);
    }

    [Fact]
    public void AddStaple_ReturnsFalse_WhenDuplicate()
    {
        var (service, _) = CreateService();
        service.AddStaple("tahini");
        var result = service.AddStaple("Tahini"); // case-insensitive duplicate
        Assert.False(result);
    }

    [Fact]
    public void AddStaple_NormalizesToLowercase()
    {
        var (service, _) = CreateService();
        service.AddStaple("KALE");
        Assert.Contains("kale", service.GetPantryStaples());
    }

    [Fact]
    public void AddStaple_PersistsToDisk()
    {
        var (service, mock) = CreateService();
        service.AddStaple("tahini");
        mock.Verify(s => s.Save(It.IsAny<AppData>()), Times.AtLeastOnce);
    }

    // ── Remove Staple ─────────────────────────────────────────────────────────

    [Fact]
    public void RemoveStaple_ReturnsTrue_WhenFound()
    {
        var (service, _) = CreateService();
        var result = service.RemoveStaple("garlic");
        Assert.True(result);
        Assert.DoesNotContain("garlic", service.GetPantryStaples());
    }

    [Fact]
    public void RemoveStaple_ReturnsFalse_WhenNotFound()
    {
        var (service, _) = CreateService();
        var result = service.RemoveStaple("truffle oil");
        Assert.False(result);
    }

    [Fact]
    public void RemoveStaple_IsCaseInsensitive()
    {
        var (service, _) = CreateService();
        var result = service.RemoveStaple("GARLIC");
        Assert.True(result);
    }

    // ── Fridge Contents ───────────────────────────────────────────────────────

    [Fact]
    public void AddFridgeItem_StoresItem()
    {
        var (service, _) = CreateService();
        service.AddFridgeItem("spinach");
        service.AddFridgeItem("chicken");
        Assert.Equal(2, service.GetFridgeContents().Count);
    }

    [Fact]
    public void AddFridgeItem_ReturnsFalse_OnDuplicate()
    {
        var (service, _) = CreateService();
        Assert.True(service.AddFridgeItem("spinach"));
        Assert.False(service.AddFridgeItem("spinach")); // duplicate
    }

    [Fact]
    public void AddFridgeItem_IsPersisted()
    {
        // Fridge contents are persisted immediately
        var (service, mock) = CreateService();
        service.AddFridgeItem("spinach");
        mock.Verify(s => s.Save(It.IsAny<AppData>()), Times.Once);
    }

    [Fact]
    public void RemoveFridgeItem_RemovesItem()
    {
        var (service, _) = CreateService();
        service.AddFridgeItem("spinach");
        service.RemoveFridgeItem("spinach");
        Assert.Empty(service.GetFridgeContents());
    }

    [Fact]
    public void ClearFridgeContents_EmptiesTheList()
    {
        var (service, _) = CreateService();
        service.AddFridgeItem("spinach");
        service.AddFridgeItem("chicken");
        service.ClearFridgeContents();
        Assert.Empty(service.GetFridgeContents());
    }

    // ── Combined Ingredients ──────────────────────────────────────────────────

    [Fact]
    public void GetAllIngredients_CombinesPantryAndFridge()
    {
        var (service, _) = CreateService();
        service.AddFridgeItem("chicken breast");

        var all = service.GetAllIngredients();

        Assert.Contains("garlic", all);          // from pantry
        Assert.Contains("chicken breast", all);  // from fridge
    }

    [Fact]
    public void GetAllIngredients_DeduplicatesAcrossLists()
    {
        var (service, _) = CreateService();
        // "garlic" is in both pantry defaults and fridge
        service.AddFridgeItem("garlic");
        service.AddFridgeItem("spinach");

        var all = service.GetAllIngredients();
        Assert.Single(all.Where(i => i == "garlic"));
    }

    // ── Reset to Defaults ─────────────────────────────────────────────────────

    [Fact]
    public void ResetToDefaults_RestoresDefaultList()
    {
        var (service, _) = CreateService();
        service.AddStaple("truffle oil");
        service.ResetToDefaults();

        Assert.Equal(PantryService.DefaultStaples.Count, service.GetPantryStaples().Count);
        Assert.DoesNotContain("truffle oil", service.GetPantryStaples());
    }
}
