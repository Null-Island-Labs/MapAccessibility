using System.Drawing;
using System.Text.Json;

namespace MapAccessibility.Tests;

public class CvdSimulatorTests
{
    [Fact]
    public void SimulateCvd_FromTestVectors()
    {
        var vectors = TestHelpers.GetSection("cvd_simulation");

        foreach (var v in vectors.EnumerateArray())
        {
            string input = v.GetProperty("input").GetString()!;
            string desc = v.GetProperty("description").GetString()!;

            foreach (var cvdType in new[] { CvdType.Protanopia, CvdType.Deuteranopia, CvdType.Tritanopia })
            {
                string key = cvdType.ToString().ToLowerInvariant();
                string expectedHex = v.GetProperty(key).GetString()!;
                var (er, eg, eb) = ColorUtils.HexToRgb(expectedHex);

                string resultHex = CvdSimulator.SimulateCvd(input, cvdType);
                var (ar, ag, ab) = ColorUtils.HexToRgb(resultHex);

                Assert.True(
                    Math.Abs(ar - er) <= TestHelpers.RgbChannelTolerance &&
                    Math.Abs(ag - eg) <= TestHelpers.RgbChannelTolerance &&
                    Math.Abs(ab - eb) <= TestHelpers.RgbChannelTolerance,
                    $"{desc} [{cvdType}]: Expected {expectedHex} (RGB {er},{eg},{eb}), got {resultHex} (RGB {ar},{ag},{ab})");
            }
        }
    }

    [Fact]
    public void SimulateCvd_White_UnchangedForAllTypes()
    {
        foreach (var cvdType in new[] { CvdType.Protanopia, CvdType.Deuteranopia, CvdType.Tritanopia })
        {
            string result = CvdSimulator.SimulateCvd("#FFFFFF", cvdType);
            Assert.Equal("#FFFFFF", result);
        }
    }

    [Fact]
    public void SimulateCvd_Black_UnchangedForAllTypes()
    {
        foreach (var cvdType in new[] { CvdType.Protanopia, CvdType.Deuteranopia, CvdType.Tritanopia })
        {
            string result = CvdSimulator.SimulateCvd("#000000", cvdType);
            Assert.Equal("#000000", result);
        }
    }

    [Fact]
    public void SimulateCvd_ColorOverload_MatchesHex()
    {
        string fromHex = CvdSimulator.SimulateCvd("#FF0000", CvdType.Protanopia);
        string fromColor = CvdSimulator.SimulateCvd(Color.Red, CvdType.Protanopia);
        Assert.Equal(fromHex, fromColor);
    }

    [Fact]
    public void SimulatePalette_ReturnsCorrectCount()
    {
        var palette = new[] { "#FF0000", "#00FF00", "#0000FF" };
        var result = CvdSimulator.SimulatePalette(palette, CvdType.Protanopia);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void SimulatePalette_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CvdSimulator.SimulatePalette(null!, CvdType.Protanopia));
    }

    [Fact]
    public void SimulatePalette_EachColorMatchesIndividual()
    {
        var palette = new[] { "#FF0000", "#00FF00", "#0000FF" };
        var result = CvdSimulator.SimulatePalette(palette, CvdType.Deuteranopia);

        for (int i = 0; i < palette.Length; i++)
        {
            string individual = CvdSimulator.SimulateCvd(palette[i], CvdType.Deuteranopia);
            Assert.Equal(individual, result[i]);
        }
    }

    [Fact]
    public void IsDistinguishable_BlackVsWhite_AllTypes()
    {
        foreach (var cvdType in new[] { CvdType.Protanopia, CvdType.Deuteranopia, CvdType.Tritanopia })
        {
            Assert.True(CvdSimulator.IsDistinguishable("#000000", "#FFFFFF", cvdType));
        }
    }

    [Fact]
    public void IsDistinguishable_IdenticalColors_ReturnsFalse()
    {
        foreach (var cvdType in new[] { CvdType.Protanopia, CvdType.Deuteranopia, CvdType.Tritanopia })
        {
            Assert.False(CvdSimulator.IsDistinguishable("#FF0000", "#FF0000", cvdType));
        }
    }

    [Fact]
    public void IsDistinguishable_ColorOverload_MatchesHex()
    {
        bool fromHex = CvdSimulator.IsDistinguishable("#FF0000", "#0000FF", CvdType.Tritanopia);
        bool fromColor = CvdSimulator.IsDistinguishable(Color.Red, Color.Blue, CvdType.Tritanopia);
        Assert.Equal(fromHex, fromColor);
    }

    [Fact]
    public void SimulateCvd_InvalidHex_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CvdSimulator.SimulateCvd("ZZZZZZ", CvdType.Protanopia));
    }
}
