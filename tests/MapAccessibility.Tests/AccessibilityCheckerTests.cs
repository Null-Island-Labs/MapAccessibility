using System.Drawing;
using System.Text.Json;

namespace MapAccessibility.Tests;

public class AccessibilityCheckerTests
{
    [Fact]
    public void ContrastRatioTests_FromTestVectors()
    {
        var vectors = TestHelpers.GetSection("contrast_ratio");

        foreach (var v in vectors.EnumerateArray())
        {
            string fg = v.GetProperty("fg").GetString()!;
            string bg = v.GetProperty("bg").GetString()!;
            double expectedRatio = v.GetProperty("expected_ratio").GetDouble();
            double expectedLumFg = v.GetProperty("expected_luminance_fg").GetDouble();
            double expectedLumBg = v.GetProperty("expected_luminance_bg").GetDouble();
            bool meetsAaNormal = v.GetProperty("meets_aa_normal").GetBoolean();
            bool meetsAaLarge = v.GetProperty("meets_aa_large").GetBoolean();
            bool meetsAaaNormal = v.GetProperty("meets_aaa_normal").GetBoolean();
            bool meetsAaaLarge = v.GetProperty("meets_aaa_large").GetBoolean();
            string desc = v.GetProperty("description").GetString()!;

            // Test luminance
            double lumFg = AccessibilityChecker.RelativeLuminance(fg);
            double lumBg = AccessibilityChecker.RelativeLuminance(bg);
            Assert.True(
                Math.Abs(lumFg - expectedLumFg) <= TestHelpers.LuminanceTolerance,
                $"{desc}: Luminance fg expected {expectedLumFg}, got {lumFg}");
            Assert.True(
                Math.Abs(lumBg - expectedLumBg) <= TestHelpers.LuminanceTolerance,
                $"{desc}: Luminance bg expected {expectedLumBg}, got {lumBg}");

            // Test contrast ratio
            double ratio = AccessibilityChecker.ContrastRatio(fg, bg);
            Assert.True(
                Math.Abs(ratio - expectedRatio) <= TestHelpers.ContrastRatioTolerance,
                $"{desc}: Contrast ratio expected {expectedRatio}, got {ratio}");

            // Test WCAG checks
            Assert.Equal(meetsAaNormal, AccessibilityChecker.CheckWcag(fg, bg, WcagLevel.AA, TextSize.Normal));
            Assert.Equal(meetsAaLarge, AccessibilityChecker.CheckWcag(fg, bg, WcagLevel.AA, TextSize.Large));
            Assert.Equal(meetsAaaNormal, AccessibilityChecker.CheckWcag(fg, bg, WcagLevel.AAA, TextSize.Normal));
            Assert.Equal(meetsAaaLarge, AccessibilityChecker.CheckWcag(fg, bg, WcagLevel.AAA, TextSize.Large));

            // Test FullCheck
            var result = AccessibilityChecker.FullCheck(fg, bg);
            Assert.True(
                Math.Abs(result.Ratio - expectedRatio) <= TestHelpers.ContrastRatioTolerance,
                $"{desc}: FullCheck ratio expected {expectedRatio}, got {result.Ratio}");
            Assert.Equal(meetsAaNormal, result.MeetsAaNormal);
            Assert.Equal(meetsAaLarge, result.MeetsAaLarge);
            Assert.Equal(meetsAaaNormal, result.MeetsAaaNormal);
            Assert.Equal(meetsAaaLarge, result.MeetsAaaLarge);
        }
    }

    [Fact]
    public void RelativeLuminance_Black_IsZero()
    {
        Assert.Equal(0.0, AccessibilityChecker.RelativeLuminance("#000000"), 4);
    }

    [Fact]
    public void RelativeLuminance_White_IsOne()
    {
        Assert.Equal(1.0, AccessibilityChecker.RelativeLuminance("#FFFFFF"), 4);
    }

    [Fact]
    public void RelativeLuminance_RgbOverload_MatchesHex()
    {
        double fromHex = AccessibilityChecker.RelativeLuminance("#1B4F72");
        double fromRgb = AccessibilityChecker.RelativeLuminance(27, 79, 114);
        Assert.Equal(fromHex, fromRgb, 10);
    }

    [Fact]
    public void RelativeLuminance_ColorOverload_MatchesHex()
    {
        double fromHex = AccessibilityChecker.RelativeLuminance("#1B4F72");
        double fromColor = AccessibilityChecker.RelativeLuminance(Color.FromArgb(27, 79, 114));
        Assert.Equal(fromHex, fromColor, 10);
    }

    [Fact]
    public void ContrastRatio_IsSymmetric()
    {
        double ratio1 = AccessibilityChecker.ContrastRatio("#1B4F72", "#FFFFFF");
        double ratio2 = AccessibilityChecker.ContrastRatio("#FFFFFF", "#1B4F72");
        Assert.Equal(ratio1, ratio2, 10);
    }

    [Fact]
    public void ContrastRatio_ColorOverload_MatchesHex()
    {
        double fromHex = AccessibilityChecker.ContrastRatio("#000000", "#FFFFFF");
        double fromColor = AccessibilityChecker.ContrastRatio(Color.Black, Color.White);
        Assert.Equal(fromHex, fromColor, 10);
    }

    [Fact]
    public void ContrastRatio_BlackVsWhite_Is21()
    {
        double ratio = AccessibilityChecker.ContrastRatio("#000000", "#FFFFFF");
        Assert.Equal(21.0, ratio, TestHelpers.ContrastRatioTolerance);
    }

    [Fact]
    public void ContrastRatio_SameColor_Is1()
    {
        double ratio = AccessibilityChecker.ContrastRatio("#808080", "#808080");
        Assert.Equal(1.0, ratio, TestHelpers.ContrastRatioTolerance);
    }

    [Fact]
    public void CheckWcag_DefaultIsAaNormal()
    {
        // Dark blue on white should pass AA normal (ratio ~10.53)
        Assert.True(AccessibilityChecker.CheckWcag("#1B4F72", "#FFFFFF"));
    }

    [Fact]
    public void CheckWcag_InvalidHex_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AccessibilityChecker.CheckWcag("ZZZZZZ", "#FFFFFF"));
    }

    [Fact]
    public void FullCheck_ColorOverload_MatchesHex()
    {
        var fromHex = AccessibilityChecker.FullCheck("#000000", "#FFFFFF");
        var fromColor = AccessibilityChecker.FullCheck(Color.Black, Color.White);
        Assert.Equal(fromHex.Ratio, fromColor.Ratio, 10);
        Assert.Equal(fromHex.MeetsAaNormal, fromColor.MeetsAaNormal);
    }
}
