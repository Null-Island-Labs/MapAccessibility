using System.Drawing;

namespace MapAccessibility.Tests;

public class ColorUtilsTests
{
    [Theory]
    [InlineData("#000000", 0, 0, 0)]
    [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("#1B4F72", 27, 79, 114)]
    [InlineData("1B4F72", 27, 79, 114)]
    [InlineData("#F00", 255, 0, 0)]
    [InlineData("F00", 255, 0, 0)]
    [InlineData("#ff0000", 255, 0, 0)]
    public void HexToRgb_ValidInput_ReturnsCorrectTuple(string hex, int r, int g, int b)
    {
        var result = ColorUtils.HexToRgb(hex);
        Assert.Equal((r, g, b), result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ZZZ")]
    [InlineData("#GGGGGG")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    public void HexToRgb_InvalidInput_ThrowsArgumentException(string hex)
    {
        Assert.Throws<ArgumentException>(() => ColorUtils.HexToRgb(hex));
    }

    [Fact]
    public void HexToRgb_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ColorUtils.HexToRgb(null!));
    }

    [Theory]
    [InlineData(0, 0, 0, "#000000")]
    [InlineData(255, 255, 255, "#FFFFFF")]
    [InlineData(27, 79, 114, "#1B4F72")]
    [InlineData(255, 0, 0, "#FF0000")]
    public void RgbToHex_ValidInput_ReturnsCorrectHex(int r, int g, int b, string expected)
    {
        Assert.Equal(expected, ColorUtils.RgbToHex(r, g, b));
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, 256, 0)]
    [InlineData(0, 0, 300)]
    public void RgbToHex_OutOfRange_ThrowsArgumentOutOfRangeException(int r, int g, int b)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ColorUtils.RgbToHex(r, g, b));
    }

    [Fact]
    public void NormalizeColor_HexString_ReturnsSameAsHexToRgb()
    {
        var result = ColorUtils.NormalizeColor("#1B4F72");
        Assert.Equal((27, 79, 114), result);
    }

    [Fact]
    public void NormalizeColor_Tuple_ValidatesAndReturns()
    {
        var result = ColorUtils.NormalizeColor((100, 200, 50));
        Assert.Equal((100, 200, 50), result);
    }

    [Fact]
    public void NormalizeColor_Tuple_InvalidChannel_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ColorUtils.NormalizeColor((256, 0, 0)));
    }

    [Fact]
    public void NormalizeColor_SystemDrawingColor_Works()
    {
        var color = Color.FromArgb(27, 79, 114);
        var result = ColorUtils.NormalizeColor(color);
        Assert.Equal((27, 79, 114), result);
    }

    [Theory]
    [InlineData("#000000", true)]
    [InlineData("#FFF", true)]
    [InlineData("1B4F72", true)]
    [InlineData("ZZZ", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("#12345", false)]
    public void IsValidHex_ReturnsExpected(string? hex, bool expected)
    {
        Assert.Equal(expected, ColorUtils.IsValidHex(hex));
    }

    [Fact]
    public void SrgbToLinear_BelowThreshold_DividesByTwelveNineTwoTwo()
    {
        // 0.04045 / 12.92 ≈ 0.003130804953560372
        double result = ColorUtils.SrgbToLinear(0.04045);
        Assert.Equal(0.04045 / 12.92, result, 10);
    }

    [Fact]
    public void SrgbToLinear_AboveThreshold_AppliesGammaExpansion()
    {
        // (0.5 + 0.055) / 1.055 ^ 2.4
        double result = ColorUtils.SrgbToLinear(0.5);
        double expected = Math.Pow((0.5 + 0.055) / 1.055, 2.4);
        Assert.Equal(expected, result, 10);
    }

    [Fact]
    public void SrgbToLinear_Zero_ReturnsZero()
    {
        Assert.Equal(0.0, ColorUtils.SrgbToLinear(0.0));
    }

    [Fact]
    public void SrgbToLinear_One_ReturnsOne()
    {
        Assert.Equal(1.0, ColorUtils.SrgbToLinear(1.0), 10);
    }

    [Fact]
    public void LinearToSrgb_RoundTrips()
    {
        // Round-trip: SrgbToLinear then LinearToSrgb should return original value
        // Precision is 6 decimal places because the piecewise boundary (0.04045)
        // causes slightly different code paths in each direction.
        double[] samples = [0.0, 0.01, 0.04045, 0.1, 0.5, 0.9, 1.0];
        foreach (double s in samples)
        {
            double roundTripped = ColorUtils.LinearToSrgb(ColorUtils.SrgbToLinear(s));
            Assert.Equal(s, roundTripped, 6);
        }
    }

    [Fact]
    public void HexToRgb_ShortForm_ExpandsCorrectly()
    {
        // #ABC -> #AABBCC -> (170, 187, 204)
        var result = ColorUtils.HexToRgb("#ABC");
        Assert.Equal((170, 187, 204), result);
    }
}
