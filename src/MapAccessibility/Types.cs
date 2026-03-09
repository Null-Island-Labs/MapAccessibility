using System.Drawing;

namespace MapAccessibility;

/// <summary>
/// WCAG conformance level.
/// </summary>
public enum WcagLevel
{
    /// <summary>WCAG 2.1 Level AA — minimum conformance.</summary>
    AA,
    /// <summary>WCAG 2.1 Level AAA — enhanced conformance.</summary>
    AAA
}

/// <summary>
/// Text size category for WCAG contrast thresholds.
/// </summary>
public enum TextSize
{
    /// <summary>Normal text — less than 18pt (or less than 14pt bold).</summary>
    Normal,
    /// <summary>Large text — at least 18pt (or at least 14pt bold).</summary>
    Large
}

/// <summary>
/// Color vision deficiency type (complete dichromacy).
/// </summary>
public enum CvdType
{
    /// <summary>Protanopia — no L-cones (red-blind).</summary>
    Protanopia,
    /// <summary>Deuteranopia — no M-cones (green-blind).</summary>
    Deuteranopia,
    /// <summary>Tritanopia — no S-cones (blue-blind).</summary>
    Tritanopia
}

/// <summary>
/// Result of a full WCAG contrast check between two colors.
/// </summary>
/// <param name="Ratio">Contrast ratio in the range [1.0, 21.0].</param>
/// <param name="MeetsAaNormal">Whether the pair meets WCAG AA for normal text (≥ 4.5:1).</param>
/// <param name="MeetsAaLarge">Whether the pair meets WCAG AA for large text (≥ 3.0:1).</param>
/// <param name="MeetsAaaNormal">Whether the pair meets WCAG AAA for normal text (≥ 7.0:1).</param>
/// <param name="MeetsAaaLarge">Whether the pair meets WCAG AAA for large text (≥ 4.5:1).</param>
public readonly record struct ContrastResult(
    double Ratio,
    bool MeetsAaNormal,
    bool MeetsAaLarge,
    bool MeetsAaaNormal,
    bool MeetsAaaLarge
);

/// <summary>
/// Accessibility score for a color palette.
/// </summary>
/// <param name="OverallScore">Overall accessibility score in [0.0, 1.0]. Computed as 0.6 * contrast_score + 0.4 * cvd_score.</param>
/// <param name="MinPairwiseContrast">Lowest contrast ratio between any two colors in the palette.</param>
/// <param name="MeanPairwiseContrast">Mean contrast ratio across all color pairs.</param>
/// <param name="CvdSafeCount">Number of CVD types (0–3) for which all pairs remain distinguishable.</param>
/// <param name="CvdSafeTypes">CVD types for which all pairs remain distinguishable.</param>
/// <param name="NColors">Number of colors in the palette.</param>
/// <param name="Warnings">Accessibility warnings (e.g. low contrast, CVD-unsafe).</param>
public readonly record struct PaletteScore(
    double OverallScore,
    double MinPairwiseContrast,
    double MeanPairwiseContrast,
    int CvdSafeCount,
    IReadOnlyList<CvdType> CvdSafeTypes,
    int NColors,
    IReadOnlyList<string> Warnings
);

/// <summary>
/// Metadata for a pre-built accessible palette.
/// </summary>
/// <param name="Name">Palette name (e.g. "qualitative_6", "blues").</param>
/// <param name="Category">Category: "qualitative", "sequential", or "diverging".</param>
/// <param name="Colors">Hex color strings in the palette.</param>
/// <param name="MinN">Minimum number of colors that can be subsampled.</param>
/// <param name="MaxN">Maximum number of colors available.</param>
/// <param name="AccessibilityScore">Pre-computed accessibility score [0.0, 1.0].</param>
/// <param name="CvdSafe">Whether the palette is safe for all CVD types.</param>
public readonly record struct PaletteInfo(
    string Name,
    string Category,
    IReadOnlyList<string> Colors,
    int MinN,
    int MaxN,
    double AccessibilityScore,
    bool CvdSafe
);
