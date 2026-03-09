using System.Drawing;

namespace MapAccessibility;

/// <summary>
/// WCAG 2.1 relative luminance and contrast ratio calculations.
/// Implements the exact algorithm from WCAG 2.1 §1.4.3 and Technique G18.
/// Uses the correct sRGB linearization threshold (0.04045, not 0.03928).
/// </summary>
public static class AccessibilityChecker
{
    private const double WcagAaNormalThreshold = 4.5;
    private const double WcagAaLargeThreshold = 3.0;
    private const double WcagAaaNormalThreshold = 7.0;
    private const double WcagAaaLargeThreshold = 4.5;

    /// <summary>
    /// Compute the WCAG 2.1 relative luminance of a color given as a hex string.
    /// </summary>
    /// <param name="hexColor">Hex color string (e.g. "#1B4F72").</param>
    /// <returns>Relative luminance in [0.0, 1.0] where 0 is black and 1 is white.</returns>
    /// <exception cref="ArgumentException">Thrown when the hex string is invalid.</exception>
    public static double RelativeLuminance(string hexColor)
    {
        var (r, g, b) = ColorUtils.NormalizeColor(hexColor);
        return RelativeLuminanceFromRgb(r, g, b);
    }

    /// <summary>
    /// Compute the WCAG 2.1 relative luminance of a color given as RGB channels.
    /// </summary>
    /// <param name="r">Red channel [0, 255].</param>
    /// <param name="g">Green channel [0, 255].</param>
    /// <param name="b">Blue channel [0, 255].</param>
    /// <returns>Relative luminance in [0.0, 1.0].</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any channel is outside [0, 255].</exception>
    public static double RelativeLuminance(int r, int g, int b)
    {
        ColorUtils.NormalizeColor((r, g, b));
        return RelativeLuminanceFromRgb(r, g, b);
    }

    /// <summary>
    /// Compute the WCAG 2.1 relative luminance of a <see cref="Color"/>.
    /// </summary>
    /// <param name="color">The color value.</param>
    /// <returns>Relative luminance in [0.0, 1.0].</returns>
    public static double RelativeLuminance(Color color)
        => RelativeLuminanceFromRgb(color.R, color.G, color.B);

    /// <summary>
    /// Compute the WCAG 2.1 contrast ratio between two colors given as hex strings.
    /// </summary>
    /// <param name="fg">Foreground hex color.</param>
    /// <param name="bg">Background hex color.</param>
    /// <returns>Contrast ratio in [1.0, 21.0].</returns>
    /// <exception cref="ArgumentException">Thrown when either hex string is invalid.</exception>
    public static double ContrastRatio(string fg, string bg)
    {
        double lumFg = RelativeLuminance(fg);
        double lumBg = RelativeLuminance(bg);
        return ContrastRatioFromLuminance(lumFg, lumBg);
    }

    /// <summary>
    /// Compute the WCAG 2.1 contrast ratio between two <see cref="Color"/> values.
    /// </summary>
    /// <param name="fg">Foreground color.</param>
    /// <param name="bg">Background color.</param>
    /// <returns>Contrast ratio in [1.0, 21.0].</returns>
    public static double ContrastRatio(Color fg, Color bg)
    {
        double lumFg = RelativeLuminance(fg);
        double lumBg = RelativeLuminance(bg);
        return ContrastRatioFromLuminance(lumFg, lumBg);
    }

    /// <summary>
    /// Check if two colors meet a specific WCAG contrast threshold.
    /// </summary>
    /// <param name="fg">Foreground hex color.</param>
    /// <param name="bg">Background hex color.</param>
    /// <param name="level">WCAG conformance level (AA or AAA). Defaults to AA.</param>
    /// <param name="size">Text size category (Normal or Large). Defaults to Normal.</param>
    /// <returns><c>true</c> if the contrast ratio meets or exceeds the threshold.</returns>
    /// <exception cref="ArgumentException">Thrown when either hex string is invalid.</exception>
    public static bool CheckWcag(string fg, string bg, WcagLevel level = WcagLevel.AA, TextSize size = TextSize.Normal)
    {
        double ratio = ContrastRatio(fg, bg);
        return ratio >= GetThreshold(level, size);
    }

    /// <summary>
    /// Check if two <see cref="Color"/> values meet a specific WCAG contrast threshold.
    /// </summary>
    /// <param name="fg">Foreground color.</param>
    /// <param name="bg">Background color.</param>
    /// <param name="level">WCAG conformance level (AA or AAA). Defaults to AA.</param>
    /// <param name="size">Text size category (Normal or Large). Defaults to Normal.</param>
    /// <returns><c>true</c> if the contrast ratio meets or exceeds the threshold.</returns>
    public static bool CheckWcag(Color fg, Color bg, WcagLevel level = WcagLevel.AA, TextSize size = TextSize.Normal)
    {
        double ratio = ContrastRatio(fg, bg);
        return ratio >= GetThreshold(level, size);
    }

    /// <summary>
    /// Run all WCAG contrast checks and return a <see cref="ContrastResult"/> with all thresholds.
    /// </summary>
    /// <param name="fg">Foreground hex color.</param>
    /// <param name="bg">Background hex color.</param>
    /// <returns>A <see cref="ContrastResult"/> containing the ratio and all threshold results.</returns>
    /// <exception cref="ArgumentException">Thrown when either hex string is invalid.</exception>
    public static ContrastResult FullCheck(string fg, string bg)
    {
        double ratio = ContrastRatio(fg, bg);
        return BuildContrastResult(ratio);
    }

    /// <summary>
    /// Run all WCAG contrast checks for two <see cref="Color"/> values.
    /// </summary>
    /// <param name="fg">Foreground color.</param>
    /// <param name="bg">Background color.</param>
    /// <returns>A <see cref="ContrastResult"/> containing the ratio and all threshold results.</returns>
    public static ContrastResult FullCheck(Color fg, Color bg)
    {
        double ratio = ContrastRatio(fg, bg);
        return BuildContrastResult(ratio);
    }

    private static double RelativeLuminanceFromRgb(int r, int g, int b)
    {
        double rLin = ColorUtils.SrgbToLinear(r / 255.0);
        double gLin = ColorUtils.SrgbToLinear(g / 255.0);
        double bLin = ColorUtils.SrgbToLinear(b / 255.0);
        return 0.2126 * rLin + 0.7152 * gLin + 0.0722 * bLin;
    }

    private static double ContrastRatioFromLuminance(double lum1, double lum2)
    {
        double lighter = Math.Max(lum1, lum2);
        double darker = Math.Min(lum1, lum2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double GetThreshold(WcagLevel level, TextSize size) => (level, size) switch
    {
        (WcagLevel.AA, TextSize.Normal) => WcagAaNormalThreshold,
        (WcagLevel.AA, TextSize.Large) => WcagAaLargeThreshold,
        (WcagLevel.AAA, TextSize.Normal) => WcagAaaNormalThreshold,
        (WcagLevel.AAA, TextSize.Large) => WcagAaaLargeThreshold,
        _ => throw new ArgumentOutOfRangeException(nameof(level), $"Unknown WCAG level/size combination: {level}/{size}")
    };

    private static ContrastResult BuildContrastResult(double ratio) => new(
        Ratio: ratio,
        MeetsAaNormal: ratio >= WcagAaNormalThreshold,
        MeetsAaLarge: ratio >= WcagAaLargeThreshold,
        MeetsAaaNormal: ratio >= WcagAaaNormalThreshold,
        MeetsAaaLarge: ratio >= WcagAaaLargeThreshold
    );
}
