# Copilot Instructions — MapAccessibility

WCAG 2.1 color accessibility checking for cartographic palettes. .NET 8, zero external dependencies.

## Critical Rules

- ZERO external dependencies. System.Drawing.Color and stdlib only.
- All public types and members must have XML doc comments.
- Use 0.04045 for sRGB linearization threshold (NOT 0.03928).
- All dataclass equivalents are readonly record struct or sealed record.
- No System.Console output. This is a library.
- Nullable reference types enabled. No suppressions without justification.
- Target net8.0.

## Architecture

Five static classes, each with a single responsibility:

1. AccessibilityChecker — WCAG 2.1 contrast ratio + luminance
2. CvdSimulator — Color vision deficiency simulation (Viénot/Brettel 1999 matrices)
3. PaletteScorer — Palette-level accessibility scoring (0.0–1.0)
4. AccessiblePalettes — Pre-built accessible palettes
5. ColorUtils — Hex/RGB parsing and sRGB linearization

Plus types in a Types.cs file:
- WcagLevel enum (AA, AAA)
- TextSize enum (Normal, Large)  
- CvdType enum (Protanopia, Deuteranopia, Tritanopia)
- ContrastResult readonly record struct
- PaletteScore readonly record struct
- PaletteInfo readonly record struct

## Key Algorithms

sRGB linearization: threshold is 0.04045, gamma exponent is 2.4
Luminance coefficients: 0.2126, 0.7152, 0.0722
Contrast ratio: (L_lighter + 0.05) / (L_darker + 0.05)
WCAG AA normal: >= 4.5, AA large: >= 3.0, AAA normal: >= 7.0, AAA large: >= 4.5
Palette scoring: 0.6 * contrast_score + 0.4 * cvd_score