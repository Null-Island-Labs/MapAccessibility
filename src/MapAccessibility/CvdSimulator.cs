using System.Drawing;

namespace MapAccessibility;

/// <summary>
/// Color vision deficiency simulation using Viénot, Brettel &amp; Mollon (1999) matrices.
/// Simulates how colors appear to people with protanopia, deuteranopia, and tritanopia
/// (complete dichromacy) in linear sRGB space.
/// </summary>
public static class CvdSimulator
{
    // Viénot, Brettel & Mollon (1999) simulation matrices for complete dichromacy.
    // Each is a 3×3 matrix applied to linear RGB [R, G, B] column vector.

    private static readonly double[][] ProtanopiaMatrix =
    [
        [0.152286, 1.052583, -0.204868],
        [0.114503, 0.786281,  0.099216],
        [-0.003882, -0.048116, 1.051998],
    ];

    private static readonly double[][] DeuteranopiaMatrix =
    [
        [0.367322, 0.860646, -0.227968],
        [0.280085, 0.672501,  0.047413],
        [-0.011820, 0.042940, 0.968881],
    ];

    private static readonly double[][] TritanopiaMatrix =
    [
        [1.255528, -0.076749, -0.178779],
        [-0.078411, 0.930809, 0.147602],
        [0.004733, 0.691367, 0.303900],
    ];

    /// <summary>
    /// Simulate how a color appears under a given color vision deficiency.
    /// </summary>
    /// <param name="hexColor">Input color as hex string.</param>
    /// <param name="cvdType">Type of color vision deficiency to simulate.</param>
    /// <returns>Simulated color as hex string in "#RRGGBB" format.</returns>
    /// <exception cref="ArgumentException">Thrown when the hex string is invalid.</exception>
    public static string SimulateCvd(string hexColor, CvdType cvdType)
    {
        var (r, g, b) = ColorUtils.NormalizeColor(hexColor);
        return SimulateCvdFromRgb(r, g, b, cvdType);
    }

    /// <summary>
    /// Simulate how a <see cref="Color"/> appears under a given color vision deficiency.
    /// </summary>
    /// <param name="color">Input color.</param>
    /// <param name="cvdType">Type of color vision deficiency to simulate.</param>
    /// <returns>Simulated color as hex string in "#RRGGBB" format.</returns>
    public static string SimulateCvd(Color color, CvdType cvdType)
        => SimulateCvdFromRgb(color.R, color.G, color.B, cvdType);

    /// <summary>
    /// Simulate an entire palette under a given color vision deficiency.
    /// </summary>
    /// <param name="colors">List of hex color strings.</param>
    /// <param name="cvdType">Type of color vision deficiency to simulate.</param>
    /// <returns>List of simulated hex color strings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="colors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any hex string is invalid.</exception>
    public static IReadOnlyList<string> SimulatePalette(IReadOnlyList<string> colors, CvdType cvdType)
    {
        ArgumentNullException.ThrowIfNull(colors);
        var result = new string[colors.Count];
        for (int i = 0; i < colors.Count; i++)
            result[i] = SimulateCvd(colors[i], cvdType);
        return result;
    }

    /// <summary>
    /// Check if two colors remain distinguishable under a given color vision deficiency.
    /// Uses Euclidean distance in linear RGB space (not full CIEDE2000).
    /// </summary>
    /// <param name="color1">First hex color string.</param>
    /// <param name="color2">Second hex color string.</param>
    /// <param name="cvdType">Type of color vision deficiency.</param>
    /// <param name="minDeltaE">Minimum Euclidean distance threshold in linear RGB (scaled to 0–255). Default is 10.0, tuned for map fills.</param>
    /// <returns><c>true</c> if the two colors remain distinguishable under the CVD type.</returns>
    /// <exception cref="ArgumentException">Thrown when either hex string is invalid.</exception>
    public static bool IsDistinguishable(string color1, string color2, CvdType cvdType, double minDeltaE = 10.0)
    {
        var (r1, g1, b1) = ColorUtils.NormalizeColor(color1);
        var (r2, g2, b2) = ColorUtils.NormalizeColor(color2);
        return IsDistinguishableFromRgb(r1, g1, b1, r2, g2, b2, cvdType, minDeltaE);
    }

    /// <summary>
    /// Check if two <see cref="Color"/> values remain distinguishable under a given color vision deficiency.
    /// </summary>
    /// <param name="color1">First color.</param>
    /// <param name="color2">Second color.</param>
    /// <param name="cvdType">Type of color vision deficiency.</param>
    /// <param name="minDeltaE">Minimum Euclidean distance threshold in linear RGB (scaled to 0–255). Default is 10.0.</param>
    /// <returns><c>true</c> if the two colors remain distinguishable under the CVD type.</returns>
    public static bool IsDistinguishable(Color color1, Color color2, CvdType cvdType, double minDeltaE = 10.0)
        => IsDistinguishableFromRgb(color1.R, color1.G, color1.B, color2.R, color2.G, color2.B, cvdType, minDeltaE);

    private static string SimulateCvdFromRgb(int r, int g, int b, CvdType cvdType)
    {
        // Convert sRGB [0,255] to linear RGB [0,1]
        double rLin = ColorUtils.SrgbToLinear(r / 255.0);
        double gLin = ColorUtils.SrgbToLinear(g / 255.0);
        double bLin = ColorUtils.SrgbToLinear(b / 255.0);

        // Apply CVD simulation matrix
        double[][] matrix = GetMatrix(cvdType);
        double rSim = matrix[0][0] * rLin + matrix[0][1] * gLin + matrix[0][2] * bLin;
        double gSim = matrix[1][0] * rLin + matrix[1][1] * gLin + matrix[1][2] * bLin;
        double bSim = matrix[2][0] * rLin + matrix[2][1] * gLin + matrix[2][2] * bLin;

        // Clamp to [0, 1]
        rSim = Math.Clamp(rSim, 0.0, 1.0);
        gSim = Math.Clamp(gSim, 0.0, 1.0);
        bSim = Math.Clamp(bSim, 0.0, 1.0);

        // Convert back to sRGB and round to [0, 255]
        int rOut = (int)Math.Round(ColorUtils.LinearToSrgb(rSim) * 255.0);
        int gOut = (int)Math.Round(ColorUtils.LinearToSrgb(gSim) * 255.0);
        int bOut = (int)Math.Round(ColorUtils.LinearToSrgb(bSim) * 255.0);

        rOut = Math.Clamp(rOut, 0, 255);
        gOut = Math.Clamp(gOut, 0, 255);
        bOut = Math.Clamp(bOut, 0, 255);

        return ColorUtils.RgbToHex(rOut, gOut, bOut);
    }

    private static bool IsDistinguishableFromRgb(
        int r1, int g1, int b1,
        int r2, int g2, int b2,
        CvdType cvdType, double minDeltaE)
    {
        // Simulate both colors
        string sim1Hex = SimulateCvdFromRgb(r1, g1, b1, cvdType);
        string sim2Hex = SimulateCvdFromRgb(r2, g2, b2, cvdType);

        // Parse simulated colors back to RGB for distance calculation
        var (sr1, sg1, sb1) = ColorUtils.HexToRgb(sim1Hex);
        var (sr2, sg2, sb2) = ColorUtils.HexToRgb(sim2Hex);

        // Euclidean distance in RGB space (0-255 scale)
        double dr = sr1 - sr2;
        double dg = sg1 - sg2;
        double db = sb1 - sb2;
        double distance = Math.Sqrt(dr * dr + dg * dg + db * db);

        return distance >= minDeltaE;
    }

    private static double[][] GetMatrix(CvdType cvdType) => cvdType switch
    {
        CvdType.Protanopia => ProtanopiaMatrix,
        CvdType.Deuteranopia => DeuteranopiaMatrix,
        CvdType.Tritanopia => TritanopiaMatrix,
        _ => throw new ArgumentOutOfRangeException(nameof(cvdType), cvdType, $"Unknown CVD type: {cvdType}")
    };
}
