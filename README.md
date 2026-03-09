
# MapAccessibility

**WCAG 2.1 color accessibility checking for cartographic palettes.**

[![NuGet](https://img.shields.io/nuget/v/MapAccessibility)](https://www.nuget.org/packages/MapAccessibility/)
[![CI](https://github.com/null-island-labs/map-accessibility/actions/workflows/ci.yml/badge.svg)](https://github.com/null-island-labs/map-accessibility/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Check contrast ratios, simulate color vision deficiency, and score palette accessibility — all with **zero external dependencies**.

Part of the [Null Island Labs](https://github.com/null-island-labs) cross-language accessibility stack. Also available in [Python](https://github.com/null-island-labs/map-accessibility) and [R](https://github.com/null-island-labs/mapaccessibility).

## Install

```bash
dotnet add package MapAccessibility
```

## Quick Start

```csharp
using MapAccessibility;

// Check contrast ratio between two colors
double ratio = AccessibilityChecker.ContrastRatio("#1B4F72", "#FFFFFF"); // 8.72

// Does this pair meet WCAG AA for normal text?
AccessibilityChecker.CheckWcag("#1B4F72", "#FFFFFF"); // true
AccessibilityChecker.CheckWcag("#1B4F72", "#FFFFFF", WcagLevel.AAA); // true

// Simulate color vision deficiency
string simulated = CvdSimulator.SimulateCvd("#FF0000", CvdType.Deuteranopia); // "#A39000"

// Score an entire palette
PaletteScore result = PaletteScorer.ScorePalette(["#1B4F72", "#F39C12", "#27AE60", "#E74C3C", "#8E44AD"]);
Console.WriteLine(result.OverallScore);         // 0.0–1.0
Console.WriteLine(result.MinPairwiseContrast);
Console.WriteLine(result.CvdSafeTypes);
Console.WriteLine(string.Join(", ", result.Warnings));

// System.Drawing.Color overloads work too
using System.Drawing;
double lum = AccessibilityChecker.RelativeLuminance(Color.DarkBlue);
```

## Features

| Feature | What it does |
|---------|-------------|
| **Contrast ratio** | WCAG 2.1 relative luminance and contrast ratio (1:1 to 21:1) |
| **WCAG checking** | AA/AAA compliance for normal and large text |
| **CVD simulation** | Protanopia, deuteranopia, tritanopia via Viénot/Brettel matrices |
| **Palette scoring** | Score palettes 0.0–1.0 on contrast + CVD safety |
| **Pre-built palettes** | Curated accessible palettes for cartographic use |
| **Color overloads** | Accept hex strings, RGB tuples, or `System.Drawing.Color` |

## Why This Package?

Existing color accessibility tools either require heavy dependencies, focus on web UI (not cartography), or don't provide palette-level scoring. **MapAccessibility** is:

- **Zero external dependencies** — pure .NET 8 BCL. Embeds anywhere.
- **Cartography-focused** — scoring tuned for map fills and legends, not web buttons.
- **Cross-language** — identical algorithms and shared test vectors across .NET, Python, and R.
- **WCAG-correct** — uses the right sRGB linearization threshold (`0.04045`, not the common `0.03928` mistake).

## API Reference

### AccessibilityChecker

```csharp
double RelativeLuminance(string hex)                         // 0.0 (black) to 1.0 (white)
double ContrastRatio(string fg, string bg)                   // 1.0 to 21.0
bool   CheckWcag(string fg, string bg,                       // Pass/fail for specific threshold
                  WcagLevel level = AA, TextSize size = Normal)
ContrastResult FullCheck(string fg, string bg)               // All thresholds at once
```

### CvdSimulator

```csharp
string SimulateCvd(string hex, CvdType type)                         // Single color → hex
IReadOnlyList<string> SimulatePalette(IReadOnlyList<string> colors,  // Palette → list of hex
                                       CvdType type)
bool IsDistinguishable(string hex1, string hex2, CvdType type,       // Still distinct under CVD?
                        double minDeltaE = 10.0)
```

### PaletteScorer

```csharp
PaletteScore ScorePalette(IReadOnlyList<string> colors)                        // Overall score
IReadOnlyList<(int I, int J, double Ratio)> PairwiseContrasts(                 // All pairwise ratios
    IReadOnlyList<string> colors)
IReadOnlyDictionary<CvdType, bool> CheckPaletteCvd(IReadOnlyList<string> colors) // CVD safety per type
```

### AccessiblePalettes

```csharp
IReadOnlyList<string> GetPalette(string name, int? n = null)   // Get palette by name (with subsampling)
IReadOnlyList<PaletteInfo> ListPalettes(string? category = null) // Browse available palettes
IReadOnlyList<string> Qualitative(int? n = null)                 // Categorical palette shortcut
IReadOnlyList<string> Sequential(string name, int? n = null)     // Sequential palette shortcut
IReadOnlyList<string> Diverging(string name, int? n = null)      // Diverging palette shortcut
```

### Color Input

All public methods accept hex strings (`"#1B4F72"`, `"1B4F72"`, `"#1B4"`) and have `System.Drawing.Color` overloads. `ColorUtils` provides `HexToRgb`, `RgbToHex`, `NormalizeColor`, and `IsValidHex` helpers.

### Types

```csharp
enum WcagLevel   { AA, AAA }
enum TextSize    { Normal, Large }
enum CvdType     { Protanopia, Deuteranopia, Tritanopia }

readonly record struct ContrastResult(double Ratio, bool MeetsAaNormal, bool MeetsAaLarge,
                                       bool MeetsAaaNormal, bool MeetsAaaLarge);
readonly record struct PaletteScore(double OverallScore, double MinPairwiseContrast,
                                     int CvdSafeTypes, IReadOnlyList<string> Warnings);
readonly record struct PaletteInfo(string Name, string Category, int Count,
                                    IReadOnlyList<string> Colors, double AccessibilityScore);
```

## Cross-Language Compatibility

All implementations produce identical results, verified by shared test vectors (`tests/vectors/test_colors.json`).

| Language | Package | Status |
|----------|---------|--------|
| .NET | `MapAccessibility` | ✅ Implemented |
| Python | `map-accessibility` | Planned |
| R | `mapaccessibility` | Planned |

## License

MIT
