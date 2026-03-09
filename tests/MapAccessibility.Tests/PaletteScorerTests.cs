using System.Text.Json;

namespace MapAccessibility.Tests;

public class PaletteScorerTests
{
    [Fact]
    public void ScorePalette_FromTestVectors()
    {
        var vectors = TestHelpers.GetSection("palette_scoring");

        foreach (var v in vectors.EnumerateArray())
        {
            string desc = v.GetProperty("description").GetString()!;
            var colorsJson = v.GetProperty("colors");
            var colors = new List<string>();
            foreach (var c in colorsJson.EnumerateArray())
                colors.Add(c.GetString()!);

            var score = PaletteScorer.ScorePalette(colors);

            if (v.TryGetProperty("expected_min_contrast_gte", out var minGte))
            {
                Assert.True(score.MinPairwiseContrast >= minGte.GetDouble() - TestHelpers.ContrastRatioTolerance,
                    $"{desc}: Expected min contrast >= {minGte.GetDouble()}, got {score.MinPairwiseContrast}");
            }

            if (v.TryGetProperty("expected_min_contrast_lte", out var minLte))
            {
                Assert.True(score.MinPairwiseContrast <= minLte.GetDouble() + TestHelpers.ContrastRatioTolerance,
                    $"{desc}: Expected min contrast <= {minLte.GetDouble()}, got {score.MinPairwiseContrast}");
            }

            if (v.TryGetProperty("expected_overall_score_gte", out var overallGte))
            {
                Assert.True(score.OverallScore >= overallGte.GetDouble() - TestHelpers.PaletteScoreTolerance,
                    $"{desc}: Expected overall score >= {overallGte.GetDouble()}, got {score.OverallScore}");
            }

            if (v.TryGetProperty("expected_overall_score_lte", out var overallLte))
            {
                Assert.True(score.OverallScore <= overallLte.GetDouble() + TestHelpers.PaletteScoreTolerance,
                    $"{desc}: Expected overall score <= {overallLte.GetDouble()}, got {score.OverallScore}");
            }

            // NColors should match input length
            Assert.Equal(colors.Count, score.NColors);
        }
    }

    [Fact]
    public void ScorePalette_BlackAndWhite_HighScore()
    {
        var score = PaletteScorer.ScorePalette(["#000000", "#FFFFFF"]);
        Assert.True(score.OverallScore >= 0.9);
        Assert.True(score.MinPairwiseContrast >= 20.0);
        Assert.Equal(2, score.NColors);
    }

    [Fact]
    public void ScorePalette_IdenticalColors_LowScore()
    {
        var score = PaletteScorer.ScorePalette(["#FF0000", "#FF0000"]);
        Assert.Equal(1.0, score.MinPairwiseContrast, TestHelpers.ContrastRatioTolerance);
        Assert.True(score.OverallScore <= 0.15);
        Assert.Contains("very low contrast", score.Warnings[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScorePalette_LessThanTwoColors_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PaletteScorer.ScorePalette(["#000000"]));
    }

    [Fact]
    public void ScorePalette_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PaletteScorer.ScorePalette(null!));
    }

    [Fact]
    public void ScorePalette_OverallScore_InRange()
    {
        var score = PaletteScorer.ScorePalette(["#1B4F72", "#F39C12", "#27AE60", "#E74C3C", "#8E44AD"]);
        Assert.InRange(score.OverallScore, 0.0, 1.0);
        Assert.True(score.MinPairwiseContrast >= 1.0);
        Assert.True(score.MeanPairwiseContrast >= score.MinPairwiseContrast);
        Assert.InRange(score.CvdSafeCount, 0, 3);
        Assert.Equal(5, score.NColors);
    }

    [Fact]
    public void PairwiseContrasts_ReturnsCorrectPairCount()
    {
        var colors = new List<string> { "#000000", "#808080", "#FFFFFF" };
        var pairs = PaletteScorer.PairwiseContrasts(colors);
        // 3 * 2 / 2 = 3 pairs
        Assert.Equal(3, pairs.Count);
    }

    [Fact]
    public void PairwiseContrasts_IndicesAreCorrect()
    {
        var colors = new List<string> { "#000000", "#808080", "#FFFFFF" };
        var pairs = PaletteScorer.PairwiseContrasts(colors);
        Assert.Equal((0, 1), (pairs[0].I, pairs[0].J));
        Assert.Equal((0, 2), (pairs[1].I, pairs[1].J));
        Assert.Equal((1, 2), (pairs[2].I, pairs[2].J));
    }

    [Fact]
    public void CheckPaletteCvd_DefaultChecksAllThreeTypes()
    {
        var colors = new List<string> { "#000000", "#FFFFFF" };
        var result = PaletteScorer.CheckPaletteCvd(colors);
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey(CvdType.Protanopia));
        Assert.True(result.ContainsKey(CvdType.Deuteranopia));
        Assert.True(result.ContainsKey(CvdType.Tritanopia));
    }

    [Fact]
    public void CheckPaletteCvd_BlackAndWhite_AllSafe()
    {
        var colors = new List<string> { "#000000", "#FFFFFF" };
        var result = PaletteScorer.CheckPaletteCvd(colors);
        Assert.True(result[CvdType.Protanopia]);
        Assert.True(result[CvdType.Deuteranopia]);
        Assert.True(result[CvdType.Tritanopia]);
    }

    [Fact]
    public void CheckPaletteCvd_IdenticalColors_AllUnsafe()
    {
        var colors = new List<string> { "#FF0000", "#FF0000" };
        var result = PaletteScorer.CheckPaletteCvd(colors);
        Assert.False(result[CvdType.Protanopia]);
        Assert.False(result[CvdType.Deuteranopia]);
        Assert.False(result[CvdType.Tritanopia]);
    }

    [Fact]
    public void CheckPaletteCvd_SpecificTypes()
    {
        var colors = new List<string> { "#000000", "#FFFFFF" };
        var result = PaletteScorer.CheckPaletteCvd(colors, [CvdType.Protanopia]);
        Assert.Single(result);
        Assert.True(result.ContainsKey(CvdType.Protanopia));
    }

    [Fact]
    public void ScorePalette_ScoringFormula_Correct()
    {
        // Verify the 0.6 * contrast + 0.4 * cvd formula with a controlled input
        var score = PaletteScorer.ScorePalette(["#000000", "#FFFFFF"]);
        double expectedContrastScore = Math.Clamp(score.MinPairwiseContrast / 4.5, 0.0, 1.0);
        double expectedCvdScore = score.CvdSafeCount / 3.0;
        double expectedOverall = 0.6 * expectedContrastScore + 0.4 * expectedCvdScore;
        Assert.Equal(expectedOverall, score.OverallScore, 10);
    }
}
