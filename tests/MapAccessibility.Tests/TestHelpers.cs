using System.Text.Json;

namespace MapAccessibility.Tests;

/// <summary>
/// Loads shared test vectors from the cross-language JSON contract file.
/// </summary>
internal static class TestHelpers
{
    private static readonly Lazy<JsonDocument> Doc = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestVectors", "test_colors.json");
        string json = File.ReadAllText(path);
        return JsonDocument.Parse(json);
    });

    public static JsonElement GetSection(string name) => Doc.Value.RootElement.GetProperty(name);

    /// <summary>Tolerance for contrast ratio comparisons.</summary>
    public const double ContrastRatioTolerance = 0.01;

    /// <summary>Tolerance for luminance comparisons.</summary>
    public const double LuminanceTolerance = 0.0001;

    /// <summary>Tolerance for RGB channel comparisons in CVD simulation.</summary>
    public const int RgbChannelTolerance = 2;

    /// <summary>Tolerance for palette score comparisons.</summary>
    public const double PaletteScoreTolerance = 0.05;
}
