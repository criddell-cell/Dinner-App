using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using DinnerPicker.Models;
using DinnerPicker.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace DinnerPicker.Tests;

/// <summary>
/// Tests for SuggestionService, focusing on response validation and error handling.
/// HTTP calls are intercepted via a mocked HttpMessageHandler.
/// </summary>
public class SuggestionServiceTests
{
    private const string ValidApiKey = "sk-ant-test-key";

    // ── Test Fixtures ─────────────────────────────────────────────────────────

    private static List<MealSuggestion> ValidSuggestions() =>
    [
        new()
        {
            Id = 1,
            Name = "Garlic Fried Rice",
            Cuisine = "Asian",
            EstimatedMinutes = 15,
            Ingredients = ["rice", "garlic", "eggs", "soy sauce"],
            MissingIngredients = [],
            Instructions = "Heat oil, fry garlic, add rice and eggs, season with soy sauce."
        },
        new()
        {
            Id = 2,
            Name = "Spaghetti Aglio e Olio",
            Cuisine = "Italian",
            EstimatedMinutes = 20,
            Ingredients = ["pasta", "garlic", "olive oil"],
            MissingIngredients = [],
            Instructions = "Cook pasta, sauté garlic in oil, toss together and season."
        },
        new()
        {
            Id = 3,
            Name = "Chicken Tacos",
            Cuisine = "Mexican",
            EstimatedMinutes = 25,
            Ingredients = ["garlic", "onion", "hot sauce", "chicken breast", "corn tortillas", "lime"],
            MissingIngredients = ["chicken breast", "corn tortillas", "lime"],
            Instructions = "Season and cook chicken, warm tortillas, assemble with toppings."
        }
    ];

    private static string BuildClaudeResponse(List<MealSuggestion> suggestions)
    {
        var body = new SuggestionResponse { Suggestions = suggestions };
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Wrap in Claude API response envelope
        return JsonSerializer.Serialize(new
        {
            content = new[] { new { text = json } }
        });
    }

    private static SuggestionService CreateServiceWithResponse(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", ValidApiKey);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new SuggestionService(factoryMock.Object);
    }

    // ── Happy Path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsThreeSuggestions_OnValidResponse()
    {
        var service = CreateServiceWithResponse(BuildClaudeResponse(ValidSuggestions()));

        var result = await service.GetSuggestionsAsync(
            ["garlic", "rice", "pasta", "eggs"],
            new QuizAnswers(),
            []);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetSuggestionsAsync_FirstTwoHaveNoMissingIngredients()
    {
        var service = CreateServiceWithResponse(BuildClaudeResponse(ValidSuggestions()));

        var result = await service.GetSuggestionsAsync([], new QuizAnswers(), []);

        Assert.Empty(result[0].MissingIngredients);
        Assert.Empty(result[1].MissingIngredients);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ThirdHasOneToThreeMissingIngredients()
    {
        var service = CreateServiceWithResponse(BuildClaudeResponse(ValidSuggestions()));

        var result = await service.GetSuggestionsAsync([], new QuizAnswers(), []);

        Assert.InRange(result[2].MissingIngredients.Count, 1, 3);
    }

    // ── Validation Failures ───────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestionsAsync_Throws_WhenOnlyTwoSuggestionsReturned()
    {
        var twoSuggestions = ValidSuggestions().Take(2).ToList();
        var service = CreateServiceWithResponse(BuildClaudeResponse(twoSuggestions));

        await Assert.ThrowsAsync<SuggestionException>(() =>
            service.GetSuggestionsAsync([], new QuizAnswers(), []));
    }

    [Fact]
    public async Task GetSuggestionsAsync_Throws_WhenFirstSuggestionHasMissingIngredients()
    {
        var invalid = ValidSuggestions();
        invalid[0].MissingIngredients = ["extra item"]; // violates 2+1 rule

        var service = CreateServiceWithResponse(BuildClaudeResponse(invalid));

        await Assert.ThrowsAsync<SuggestionException>(() =>
            service.GetSuggestionsAsync([], new QuizAnswers(), []));
    }

    [Fact]
    public async Task GetSuggestionsAsync_Throws_WhenThirdSuggestionHasTooManyMissingIngredients()
    {
        var invalid = ValidSuggestions();
        invalid[2].MissingIngredients = ["a", "b", "c", "d"]; // 4 items — over limit

        var service = CreateServiceWithResponse(BuildClaudeResponse(invalid));

        await Assert.ThrowsAsync<SuggestionException>(() =>
            service.GetSuggestionsAsync([], new QuizAnswers(), []));
    }

    [Fact]
    public async Task GetSuggestionsAsync_Throws_WhenThirdSuggestionHasNoMissingIngredients()
    {
        var invalid = ValidSuggestions();
        invalid[2].MissingIngredients = []; // must have 1-3

        var service = CreateServiceWithResponse(BuildClaudeResponse(invalid));

        await Assert.ThrowsAsync<SuggestionException>(() =>
            service.GetSuggestionsAsync([], new QuizAnswers(), []));
    }

    [Fact]
    public async Task GetSuggestionsAsync_Throws_WhenResponseIsNotJson()
    {
        var service = CreateServiceWithResponse(
            JsonSerializer.Serialize(new { content = new[] { new { text = "Sorry, I cannot help with that." } } }));

        await Assert.ThrowsAsync<SuggestionException>(() =>
            service.GetSuggestionsAsync([], new QuizAnswers(), []));
    }

    // ── API Error Handling ────────────────────────────────────────────────────

    [Fact]
    public async Task GetSuggestionsAsync_Throws_On401Unauthorized()
    {
        var service = CreateServiceWithResponse("{\"error\":\"unauthorized\"}", HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<SuggestionException>(() =>
            service.GetSuggestionsAsync([], new QuizAnswers(), []));
    }

    [Fact]
    public async Task GetSuggestionsAsync_Throws_On500ServerError()
    {
        var service = CreateServiceWithResponse("{\"error\":\"server error\"}", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<SuggestionException>(() =>
            service.GetSuggestionsAsync([], new QuizAnswers(), []));
    }
}
