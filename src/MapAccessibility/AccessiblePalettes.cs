namespace MapAccessibility;

/// <summary>
/// Pre-built accessible color palettes for cartographic use.
/// All palettes are designed to meet WCAG AA contrast minimums and
/// remain distinguishable under the three common color vision deficiency types.
/// </summary>
public static class AccessiblePalettes
{
    private static readonly Dictionary<string, PaletteInfo> Palettes = BuildPalettes();

    /// <summary>
    /// Get a palette by name. Optionally subsample to <paramref name="n"/> colors.
    /// </summary>
    /// <param name="name">Palette name (case-insensitive).</param>
    /// <param name="n">Optional number of colors to return. Must be between the palette's MinN and MaxN.</param>
    /// <returns>List of hex color strings.</returns>
    /// <exception cref="ArgumentException">Thrown when the palette name is unknown or <paramref name="n"/> is out of range.</exception>
    public static IReadOnlyList<string> GetPalette(string name, int? n = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        string key = name.ToUpperInvariant();

        if (!Palettes.TryGetValue(key, out var info))
            throw new ArgumentException($"Unknown palette name: '{name}'.", nameof(name));

        if (n is null)
            return info.Colors;

        if (n.Value < info.MinN || n.Value > info.MaxN)
            throw new ArgumentException(
                $"Palette '{name}' supports {info.MinN}–{info.MaxN} colors, but {n.Value} was requested.",
                nameof(n));

        // Subsample by taking evenly spaced colors
        if (n.Value == info.Colors.Count)
            return info.Colors;

        var result = new List<string>(n.Value);
        double step = (double)(info.Colors.Count - 1) / (n.Value - 1);
        for (int i = 0; i < n.Value; i++)
        {
            int index = (int)Math.Round(i * step);
            result.Add(info.Colors[index]);
        }
        return result.AsReadOnly();
    }

    /// <summary>
    /// List all available palettes, optionally filtered by category.
    /// </summary>
    /// <param name="category">Optional category filter: "qualitative", "sequential", or "diverging" (case-insensitive).</param>
    /// <returns>List of <see cref="PaletteInfo"/> records.</returns>
    public static IReadOnlyList<PaletteInfo> ListPalettes(string? category = null)
    {
        if (category is null)
            return Palettes.Values.ToList().AsReadOnly();

        string cat = category.ToUpperInvariant();
        return Palettes.Values
            .Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Get a qualitative (categorical) palette. Convenience shortcut.
    /// </summary>
    /// <param name="n">Number of colors (2–8).</param>
    /// <returns>List of hex color strings.</returns>
    public static IReadOnlyList<string> Qualitative(int n)
    {
        if (n <= 6)
            return GetPalette("qualitative_6", n);
        return GetPalette("qualitative_8", n);
    }

    /// <summary>
    /// Get a sequential palette by name. Convenience shortcut.
    /// </summary>
    /// <param name="name">Palette name (e.g. "blues", "greens").</param>
    /// <param name="n">Number of color steps (2–7).</param>
    /// <returns>List of hex color strings.</returns>
    public static IReadOnlyList<string> Sequential(string name, int n = 7)
    {
        return GetPalette(name, n);
    }

    /// <summary>
    /// Get a diverging palette by name. Convenience shortcut.
    /// </summary>
    /// <param name="name">Palette name (e.g. "red_blue", "brown_teal").</param>
    /// <param name="n">Number of color steps (2–7).</param>
    /// <returns>List of hex color strings.</returns>
    public static IReadOnlyList<string> Diverging(string name, int n = 7)
    {
        return GetPalette(name, n);
    }

    private static Dictionary<string, PaletteInfo> BuildPalettes()
    {
        var palettes = new Dictionary<string, PaletteInfo>(StringComparer.OrdinalIgnoreCase);

        // === Qualitative palettes (categorical maps) ===

        Register(palettes, "qualitative_6", "qualitative",
            ["#1B4F72", "#E67E22", "#27AE60", "#8E44AD", "#C0392B", "#2C3E50"],
            minN: 2, maxN: 6);

        Register(palettes, "qualitative_8", "qualitative",
            ["#1B4F72", "#E67E22", "#27AE60", "#8E44AD", "#C0392B", "#17A589", "#D4AC0D", "#2C3E50"],
            minN: 2, maxN: 8);

        // === Sequential palettes (continuous data) ===

        Register(palettes, "blues", "sequential",
            ["#D6EAF8", "#AED6F1", "#85C1E9", "#5DADE2", "#3498DB", "#2E86C1", "#1B4F72"],
            minN: 2, maxN: 7);

        Register(palettes, "greens", "sequential",
            ["#D5F5E3", "#ABEBC6", "#82E0AA", "#58D68D", "#2ECC71", "#28B463", "#1E8449"],
            minN: 2, maxN: 7);

        Register(palettes, "reds", "sequential",
            ["#FADBD8", "#F5B7B1", "#F1948A", "#EC7063", "#E74C3C", "#CB4335", "#922B21"],
            minN: 2, maxN: 7);

        Register(palettes, "purples", "sequential",
            ["#E8DAEF", "#D2B4DE", "#BB8FCE", "#A569BD", "#8E44AD", "#7D3C98", "#6C3483"],
            minN: 2, maxN: 7);

        // === Diverging palettes (deviation from center) ===

        Register(palettes, "red_blue", "diverging",
            ["#922B21", "#CB4335", "#E74C3C", "#F0F0F0", "#3498DB", "#2E86C1", "#1B4F72"],
            minN: 2, maxN: 7);

        Register(palettes, "brown_teal", "diverging",
            ["#7E5109", "#B9770E", "#F0B27A", "#F0F0F0", "#76D7C4", "#17A589", "#0E6655"],
            minN: 2, maxN: 7);

        return palettes;
    }

    private static void Register(
        Dictionary<string, PaletteInfo> palettes,
        string name,
        string category,
        string[] colors,
        int minN,
        int maxN)
    {
        var colorList = Array.AsReadOnly(colors);
        var score = PaletteScorer.ScorePalette(colorList);

        palettes[name.ToUpperInvariant()] = new PaletteInfo(
            Name: name,
            Category: category,
            Colors: colorList,
            MinN: minN,
            MaxN: maxN,
            AccessibilityScore: score.OverallScore,
            CvdSafe: score.CvdSafeCount == 3
        );
    }
}
