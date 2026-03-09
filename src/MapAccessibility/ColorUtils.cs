using System.Drawing;
using System.Globalization;

namespace MapAccessibility;

/// <summary>
/// Color parsing, conversion, and validation utilities.
/// Accepts hex strings and RGB tuples. Provides sRGB linearization
/// shared by <see cref="AccessibilityChecker"/> and <see cref="CvdSimulator"/>.
/// </summary>
public static class ColorUtils
{
    /// <summary>
    /// Parse a hex color string to an RGB tuple.
    /// Accepts "#RRGGBB", "RRGGBB", "#RGB", and "RGB" formats.
    /// </summary>
    /// <param name="hex">Hex color string.</param>
    /// <returns>Tuple of (R, G, B) in [0, 255].</returns>
    /// <exception cref="ArgumentException">Thrown when the hex string is invalid.</exception>
    public static (int R, int G, int B) HexToRgb(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        ReadOnlySpan<char> span = hex.AsSpan().Trim();
        if (span.Length > 0 && span[0] == '#')
            span = span[1..];

        if (span.Length == 3)
        {
            if (TryParseHexChar(span[0], out int r) &&
                TryParseHexChar(span[1], out int g) &&
                TryParseHexChar(span[2], out int b))
            {
                return (r * 17, g * 17, b * 17);
            }
        }
        else if (span.Length == 6)
        {
            if (int.TryParse(span[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) &&
                int.TryParse(span[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) &&
                int.TryParse(span[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
            {
                return (r, g, b);
            }
        }

        throw new ArgumentException($"Invalid hex color: \"{hex}\". Expected #RRGGBB, RRGGBB, #RGB, or RGB.", nameof(hex));
    }

    /// <summary>
    /// Convert an RGB tuple to a hex string in "#RRGGBB" format.
    /// </summary>
    /// <param name="r">Red channel [0, 255].</param>
    /// <param name="g">Green channel [0, 255].</param>
    /// <param name="b">Blue channel [0, 255].</param>
    /// <returns>Hex string in "#RRGGBB" format.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any channel is outside [0, 255].</exception>
    public static string RgbToHex(int r, int g, int b)
    {
        ValidateChannel(r, nameof(r));
        ValidateChannel(g, nameof(g));
        ValidateChannel(b, nameof(b));
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Normalize a hex color string to an RGB tuple.
    /// </summary>
    /// <param name="hex">Hex color string.</param>
    /// <returns>Tuple of (R, G, B) in [0, 255].</returns>
    /// <exception cref="ArgumentException">Thrown when the hex string is invalid.</exception>
    public static (int R, int G, int B) NormalizeColor(string hex) => HexToRgb(hex);

    /// <summary>
    /// Normalize an RGB tuple, validating channel ranges.
    /// </summary>
    /// <param name="rgb">RGB tuple.</param>
    /// <returns>The validated (R, G, B) tuple.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any channel is outside [0, 255].</exception>
    public static (int R, int G, int B) NormalizeColor((int R, int G, int B) rgb)
    {
        ValidateChannel(rgb.R, "R");
        ValidateChannel(rgb.G, "G");
        ValidateChannel(rgb.B, "B");
        return rgb;
    }

    /// <summary>
    /// Normalize a <see cref="Color"/> to an RGB tuple.
    /// </summary>
    /// <param name="color">The color value.</param>
    /// <returns>Tuple of (R, G, B) in [0, 255].</returns>
    public static (int R, int G, int B) NormalizeColor(Color color)
        => (color.R, color.G, color.B);

    /// <summary>
    /// Check if a string is a valid hex color.
    /// </summary>
    /// <param name="hex">String to check.</param>
    /// <returns><c>true</c> if the string is a valid hex color; otherwise <c>false</c>.</returns>
    public static bool IsValidHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        ReadOnlySpan<char> span = hex.AsSpan().Trim();
        if (span.Length > 0 && span[0] == '#')
            span = span[1..];

        if (span.Length != 3 && span.Length != 6)
            return false;

        foreach (char c in span)
        {
            if (!IsHexDigit(c))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Convert a single sRGB channel value [0, 1] to linear RGB [0, 1].
    /// Uses the IEC 61966-2-1 / WCAG 2.1 correct threshold of 0.04045.
    /// </summary>
    /// <param name="channel">sRGB channel in [0, 1].</param>
    /// <returns>Linear RGB channel in [0, 1].</returns>
    internal static double SrgbToLinear(double channel)
    {
        if (channel <= 0.04045)
            return channel / 12.92;
        return Math.Pow((channel + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// Convert a single linear RGB channel [0, 1] to sRGB [0, 1].
    /// Inverse of <see cref="SrgbToLinear"/>.
    /// </summary>
    /// <param name="channel">Linear RGB channel in [0, 1].</param>
    /// <returns>sRGB channel in [0, 1].</returns>
    internal static double LinearToSrgb(double channel)
    {
        if (channel <= 0.0031308)
            return channel * 12.92;
        return 1.055 * Math.Pow(channel, 1.0 / 2.4) - 0.055;
    }

    private static void ValidateChannel(int value, string name)
    {
        if (value < 0 || value > 255)
            throw new ArgumentOutOfRangeException(name, value, $"RGB channel {name} must be 0–255, got {value}.");
    }

    private static bool IsHexDigit(char c)
        => c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');

    private static bool TryParseHexChar(char c, out int value)
    {
        if (c >= '0' && c <= '9') { value = c - '0'; return true; }
        if (c >= 'a' && c <= 'f') { value = c - 'a' + 10; return true; }
        if (c >= 'A' && c <= 'F') { value = c - 'A' + 10; return true; }
        value = 0;
        return false;
    }
}
