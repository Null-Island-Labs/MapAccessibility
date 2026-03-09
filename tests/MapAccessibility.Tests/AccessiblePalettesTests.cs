namespace MapAccessibility.Tests;

public class AccessiblePalettesTests
{
    [Theory]
    [InlineData("qualitative_6")]
    [InlineData("qualitative_8")]
    [InlineData("blues")]
    [InlineData("greens")]
    [InlineData("reds")]
    [InlineData("purples")]
    [InlineData("red_blue")]
    [InlineData("brown_teal")]
    public void GetPalette_ReturnsNonEmptyList(string name)
    {
        var palette = AccessiblePalettes.GetPalette(name);
        Assert.True(palette.Count >= 2);
    }

    [Fact]
    public void GetPalette_IsCaseInsensitive()
    {
        var lower = AccessiblePalettes.GetPalette("blues");
        var upper = AccessiblePalettes.GetPalette("BLUES");
        Assert.Equal(lower, upper);
    }

    [Fact]
    public void GetPalette_UnknownName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AccessiblePalettes.GetPalette("nonexistent"));
    }

    [Fact]
    public void GetPalette_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AccessiblePalettes.GetPalette(null!));
    }

    [Fact]
    public void GetPalette_WithN_SubsamplesCorrectly()
    {
        var full = AccessiblePalettes.GetPalette("blues");
        var sub = AccessiblePalettes.GetPalette("blues", 3);
        Assert.Equal(3, sub.Count);
        // First and last should match the full palette
        Assert.Equal(full[0], sub[0]);
        Assert.Equal(full[^1], sub[^1]);
    }

    [Fact]
    public void GetPalette_WithN_OutOfRange_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AccessiblePalettes.GetPalette("blues", 1));
        Assert.Throws<ArgumentException>(() => AccessiblePalettes.GetPalette("blues", 100));
    }

    [Fact]
    public void ListPalettes_ReturnsAll()
    {
        var all = AccessiblePalettes.ListPalettes();
        Assert.True(all.Count >= 8); // We defined at least 8 palettes
    }

    [Fact]
    public void ListPalettes_FilterByCategory()
    {
        var qualitative = AccessiblePalettes.ListPalettes("qualitative");
        Assert.True(qualitative.Count >= 2);
        Assert.All(qualitative, p => Assert.Equal("qualitative", p.Category));

        var sequential = AccessiblePalettes.ListPalettes("sequential");
        Assert.True(sequential.Count >= 4);
        Assert.All(sequential, p => Assert.Equal("sequential", p.Category));

        var diverging = AccessiblePalettes.ListPalettes("diverging");
        Assert.True(diverging.Count >= 2);
        Assert.All(diverging, p => Assert.Equal("diverging", p.Category));
    }

    [Fact]
    public void Qualitative_ReturnsCorrectCount()
    {
        var q4 = AccessiblePalettes.Qualitative(4);
        Assert.Equal(4, q4.Count);
    }

    [Fact]
    public void Sequential_ReturnsCorrectCount()
    {
        var s5 = AccessiblePalettes.Sequential("blues", 5);
        Assert.Equal(5, s5.Count);
    }

    [Fact]
    public void Diverging_ReturnsCorrectCount()
    {
        var d5 = AccessiblePalettes.Diverging("red_blue", 5);
        Assert.Equal(5, d5.Count);
    }

    [Theory]
    [InlineData("qualitative_6")]
    [InlineData("qualitative_8")]
    [InlineData("blues")]
    [InlineData("greens")]
    [InlineData("reds")]
    [InlineData("purples")]
    [InlineData("red_blue")]
    [InlineData("brown_teal")]
    public void AllPalettes_ContainValidHexColors(string name)
    {
        var palette = AccessiblePalettes.GetPalette(name);
        foreach (var color in palette)
        {
            Assert.True(ColorUtils.IsValidHex(color), $"Invalid hex in palette {name}: {color}");
        }
    }

    [Fact]
    public void AllPalettes_HaveAccessibilityScoreSet()
    {
        var all = AccessiblePalettes.ListPalettes();
        foreach (var p in all)
        {
            Assert.InRange(p.AccessibilityScore, 0.0, 1.0);
        }
    }
}
