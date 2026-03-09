namespace MapAccessibility;

/// <summary>
/// Palette-level accessibility scoring.
/// Scores palettes on a 0.0–1.0 scale based on pairwise contrast ratios (60%)
/// and CVD distinguishability (40%).
/// </summary>
public static class PaletteScorer
{
    private static readonly CvdType[] AllCvdTypes = [CvdType.Protanopia, CvdType.Deuteranopia, CvdType.Tritanopia];

    /// <summary>
    /// Score a palette's overall accessibility.
    /// </summary>
    /// <param name="colors">List of hex color strings in the palette.</param>
    /// <returns>A <see cref="PaletteScore"/> with contrast metrics, CVD safety, and an overall score.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="colors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the palette has fewer than 2 colors or any hex string is invalid.</exception>
    public static PaletteScore ScorePalette(IReadOnlyList<string> colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Count < 2)
            throw new ArgumentException("Palette must contain at least 2 colors.", nameof(colors));

        // Compute all pairwise contrast ratios
        var pairs = PairwiseContrasts(colors);
        double minContrast = double.MaxValue;
        double sumContrast = 0.0;
        foreach (var (_, _, ratio) in pairs)
        {
            if (ratio < minContrast) minContrast = ratio;
            sumContrast += ratio;
        }
        double meanContrast = pairs.Count > 0 ? sumContrast / pairs.Count : 0.0;

        // Check CVD safety
        var cvdResults = CheckPaletteCvd(colors);
        var safeTypes = new List<CvdType>();
        foreach (var (cvdType, safe) in cvdResults)
        {
            if (safe) safeTypes.Add(cvdType);
        }
        int cvdSafeCount = safeTypes.Count;

        // Compute score components
        double contrastScore = Math.Clamp(minContrast / 4.5, 0.0, 1.0);
        double cvdScore = cvdSafeCount / 3.0;
        double overallScore = 0.6 * contrastScore + 0.4 * cvdScore;

        // Build warnings
        var warnings = new List<string>();
        if (minContrast < 3.0)
            warnings.Add("Palette has very low contrast pairs");
        else if (minContrast < 4.5)
            warnings.Add("Some pairs fail WCAG AA for normal text");
        if (cvdSafeCount == 0)
            warnings.Add("Palette is not safe for any color vision deficiency");

        return new PaletteScore(
            OverallScore: overallScore,
            MinPairwiseContrast: minContrast,
            MeanPairwiseContrast: meanContrast,
            CvdSafeCount: cvdSafeCount,
            CvdSafeTypes: safeTypes.AsReadOnly(),
            NColors: colors.Count,
            Warnings: warnings.AsReadOnly()
        );
    }

    /// <summary>
    /// Compute all pairwise contrast ratios for a list of colors.
    /// </summary>
    /// <param name="colors">List of hex color strings.</param>
    /// <returns>List of (index_i, index_j, contrast_ratio) tuples for each unique pair.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="colors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any hex string is invalid.</exception>
    public static IReadOnlyList<(int I, int J, double Ratio)> PairwiseContrasts(IReadOnlyList<string> colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        var results = new List<(int, int, double)>();
        for (int i = 0; i < colors.Count; i++)
        {
            for (int j = i + 1; j < colors.Count; j++)
            {
                double ratio = AccessibilityChecker.ContrastRatio(colors[i], colors[j]);
                results.Add((i, j, ratio));
            }
        }
        return results.AsReadOnly();
    }

    /// <summary>
    /// Check palette distinguishability under each CVD type.
    /// </summary>
    /// <param name="colors">List of hex color strings.</param>
    /// <param name="cvdTypes">CVD types to check. Defaults to all three (protanopia, deuteranopia, tritanopia).</param>
    /// <param name="minDeltaE">Minimum Euclidean distance threshold for distinguishability. Default is 10.0.</param>
    /// <returns>Dictionary mapping each CVD type to whether all pairs are distinguishable.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="colors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any hex string is invalid.</exception>
    public static IReadOnlyDictionary<CvdType, bool> CheckPaletteCvd(
        IReadOnlyList<string> colors,
        IReadOnlyList<CvdType>? cvdTypes = null,
        double minDeltaE = 10.0)
    {
        ArgumentNullException.ThrowIfNull(colors);
        var typesToCheck = cvdTypes ?? AllCvdTypes;
        var results = new Dictionary<CvdType, bool>();

        foreach (var cvdType in typesToCheck)
        {
            bool allDistinguishable = true;
            for (int i = 0; i < colors.Count && allDistinguishable; i++)
            {
                for (int j = i + 1; j < colors.Count && allDistinguishable; j++)
                {
                    if (!CvdSimulator.IsDistinguishable(colors[i], colors[j], cvdType, minDeltaE))
                        allDistinguishable = false;
                }
            }
            results[cvdType] = allDistinguishable;
        }

        return results;
    }
}
