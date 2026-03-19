using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DinnerPicker.Models;

namespace DinnerPicker.Services;

public class ScanService : IScanService
{
    private const string ModelId = "claude-sonnet-4-20250514";
    private const string ApiEndpoint = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    public ScanService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? string.Empty;
    }

    public async Task<ScanResult> ScanImageAsync(Stream imageStream, string contentType)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());

        var mediaType = contentType switch
        {
            "image/png"  => "image/png",
            "image/gif"  => "image/gif",
            "image/webp" => "image/webp",
            _            => "image/jpeg"
        };

        var requestBody = new
        {
            model = ModelId,
            max_tokens = 1024,
            system = SystemPrompt,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = mediaType,
                                data = base64
                            }
                        },
                        new
                        {
                            type = "text",
                            text = "Scan this and extract the ingredients. It may be a fridge photo or a grocery receipt."
                        }
                    }
                }
            }
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var json = JsonSerializer.Serialize(requestBody);
        var response = await client.PostAsync(ApiEndpoint,
            new StringContent(json, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        return JsonSerializer.Deserialize<ScanResult>(text, JsonOptions) ?? new ScanResult();
    }

    private const string SystemPrompt = """
        You are an ingredient and grocery scanner for Nosh, a personal dinner-picking app.
        You handle two types of scans: fridge photos and grocery receipts.

        FRIDGE PHOTO RULES:
        - Only list items you can actually see or reasonably identify in the image
        - Focus on fresh/perishable items: vegetables, fruits, proteins (meat, fish, eggs,
          dairy), fresh herbs, and opened sauces or condiments
        - Ignore background staples (ketchup bottles, soy sauce, etc.) unless clearly
          prominent or nearly empty
        - If an item is ambiguous (e.g. "green vegetable"), name it as specifically as
          you can but note uncertainty with a "?" suffix

        RECEIPT RULES:
        - Extract only food/grocery items — ignore household products, cleaning supplies,
          personal care, alcohol, and non-food items
        - Normalize brand/store names into plain ingredient names
          (e.g. "PC Free Range Lrg Eggs 12pk" → "eggs")
        - Ignore weights, prices, SKUs, loyalty points, and store metadata
        - If a receipt item is clearly a prepared/packaged meal rather than a raw
          ingredient, include it but flag it as packaged: true
        - Receipt items are assumed high confidence unless the text is garbled or cut off

        Do not invent or guess items that aren't visible or listed.
        Return ONLY a JSON object, no preamble, no markdown fences.

        Return this exact JSON shape:
        {
          "scan_type": "fridge" | "receipt",
          "ingredients": [
            {
              "name": "chicken breast",
              "category": "protein",
              "confidence": "high",
              "quantity_estimate": "2 pieces",
              "packaged": false
            }
          ],
          "scan_notes": "Brief note if lighting was poor, text was cut off, or visibility was limited"
        }

        Categories must be one of: protein, vegetable, fruit, dairy, herb, condiment, grain, packaged, other
        Confidence must be one of: high, medium, low
        """;
}
