namespace DinnerPicker.Models;

public class ScanResult
{
    public string ScanType { get; set; } = "";
    public List<ScannedIngredient> Ingredients { get; set; } = [];
    public string? ScanNotes { get; set; }
}

public class ScannedIngredient
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Confidence { get; set; } = "high";
    public string? QuantityEstimate { get; set; }
    public bool Packaged { get; set; }
}
