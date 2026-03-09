# Spec — map-accessibility

> Requirements, acceptance criteria, and constraints

---

## Overview

`map-accessibility` is a Python package for checking color accessibility in cartographic contexts. It implements WCAG 2.1 contrast ratio checking, color vision deficiency (CVD) simulation, and palette-level accessibility scoring. It is the reference implementation for the cross-language Null Island Labs accessibility stack (`mapaccessibility` in R, `MapAccessibility` in .NET).

---

## Functional Requirements

### FR-01: Contrast Ratio Calculation
- Compute WCAG 2.1 relative luminance for any sRGB color
- Compute contrast ratio between any two sRGB colors
- Return ratio as float in range [1.0, 21.0]
- Match W3C reference algorithm exactly (0.04045 threshold, not 0.03928)

### FR-02: WCAG Compliance Checking
- Check if a foreground/background pair meets WCAG AA for normal text (≥ 4.5:1)
- Check if a foreground/background pair meets WCAG AA for large text (≥ 3.0:1)
- Check if a foreground/background pair meets WCAG AAA for normal text (≥ 7.0:1)
- Check if a foreground/background pair meets WCAG AAA for large text (≥ 4.5:1)
- Return boolean or structured `ContrastResult`

### FR-03: CVD Simulation
- Simulate protanopia (no red cones)
- Simulate deuteranopia (no green cones)
- Simulate tritanopia (no blue cones)
- Accept single color or list of colors
- Return simulated colors as hex strings
- Use Viénot/Brettel matrices (1999 paper)

### FR-04: Distinguishability Under CVD
- Determine if two colors remain distinguishable under a CVD type
- Use Euclidean distance in linear RGB space
- Configurable threshold (default 10.0, tuned for map fills)

### FR-05: Palette Scoring
- Score a palette on 0.0–1.0 scale
- Scoring components: pairwise contrast (60%) + CVD safety (40%)
- Return structured `PaletteScore` with all sub-metrics
- Generate warnings for low-contrast or CVD-unsafe palettes

### FR-06: Pre-Built Palettes
- Ship qualitative palettes (4–10 colors)
- Ship sequential palettes (5–9 steps, named by hue)
- Ship diverging palettes (5–11 steps, named by endpoints)
- Every shipped palette must score ≥ 0.7 on the palette scorer
- Provide lookup by name and category

### FR-07: Color Input Flexibility
- Accept hex strings: `"#1B4F72"`, `"1B4F72"`, `"#1B4"` (3-digit shorthand)
- Accept RGB tuples: `(27, 79, 114)`
- Validate all input, raise `ValueError` on invalid colors
- Normalize internally to RGB tuples, externally to `"#RRGGBB"` hex strings

---

## Non-Functional Requirements

### NFR-01: Zero Dependencies
The package has no external dependencies. All algorithms use Python stdlib (`math`, `colorsys`, `dataclasses`, `enum`, `json`). This enables embedding in any Python environment including constrained ones (Lambda, WASM, Jupyter).

### NFR-02: Cross-Language Compatibility
Shared test vectors (`tests/vectors/test_colors.json`) are the cross-language contract. The R and .NET implementations must produce identical results within tolerance (±0.01 for ratios, ±2 for RGB channels).

### NFR-03: Type Safety
Full type annotations on all public functions. `mypy --strict` must pass with zero errors. All public types are `dataclass(frozen=True)` or `Enum`.

### NFR-04: Performance
- `contrast_ratio()` for a single pair: < 1ms
- `score_palette()` for 10 colors (45 pairs × 3 CVD types): < 50ms
- No numpy, no vectorized operations — but pure Python should be fast enough for palette-scale work (not pixel-level image processing)

### NFR-05: Python Version Support
- Minimum: Python 3.9 (for `tuple[int, int, int]` syntax in annotations)
- Tested: 3.9, 3.10, 3.11, 3.12, 3.13

### NFR-06: Packaging
- Build system: hatchling
- Published to PyPI as `map-accessibility`
- Import as `map_accessibility` (underscore, standard Python convention)

---

## Acceptance Criteria

### AC-01: Contrast ratio matches W3C reference
Given: Black (#000000) and white (#FFFFFF)
Then: `contrast_ratio("#000000", "#FFFFFF")` returns 21.0

Given: The exact same color twice
Then: `contrast_ratio("#1B4F72", "#1B4F72")` returns 1.0

Given: W3C example colors
Then: Results match the W3C WCAG 2.1 technique G18 reference values within ±0.01

### AC-02: WCAG thresholds are exact
Given: A pair with ratio 4.49
Then: `check_wcag(fg, bg, WcagLevel.AA, TextSize.NORMAL)` returns `False`

Given: A pair with ratio 4.50
Then: `check_wcag(fg, bg, WcagLevel.AA, TextSize.NORMAL)` returns `True`

### AC-03: CVD simulation round-trips correctly
Given: Pure white (#FFFFFF)
Then: All three CVD simulations return #FFFFFF (white is achromatic)

Given: Pure black (#000000)
Then: All three CVD simulations return #000000 (black is achromatic)

### AC-04: Palette scoring is bounded
Given: Any valid palette
Then: `score_palette(colors).overall_score` is in [0.0, 1.0]

Given: A palette of identical colors
Then: `score_palette(["#FF0000", "#FF0000"])` returns overall_score 0.0

### AC-05: Shared test vectors pass
Given: All entries in `tests/vectors/test_colors.json`
Then: All contrast ratios match within ±0.01
Then: All CVD simulations match within ±2 per RGB channel
Then: All palette scores match within ±0.05

### AC-06: All pre-built palettes meet quality bar
Given: Every palette returned by `list_palettes()`
Then: Its `accessibility_score` is ≥ 0.7

---

## Out of Scope (v1)

- Color naming (CSS named colors, X11 colors, Pantone)
- CIEDE2000 or other perceptual color difference metrics (requires numpy-level math)
- Anomalous trichromacy simulation (partial CVD severity)
- Image-level accessibility analysis (screenshot → report)
- Color picker UI / interactive tools
- WCAG 3.0 APCA contrast algorithm (draft, not yet a recommendation)
- Integration with specific map libraries (matplotlib, ggplot2, leaflet) — that's the job of `kartocolors` and `georender`

---

## Future Scope (v2+)

- CIEDE2000 with optional numpy acceleration
- Anomalous trichromacy (severity parameter 0.0–1.0)
- WCAG 3.0 APCA when it reaches W3C Recommendation status
- `palettes.json` export command for R/.NET consumption
- CLI tool: `map-accessibility check "#1B4F72" "#FFFFFF"`
