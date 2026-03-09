# Architecture — map-accessibility

> Module design, algorithms, data flow, and interface contracts

---

## Module Dependency Graph

```
color_utils.py          _types.py
    │                       │
    ├───────────┬───────────┤
    ▼           ▼           │
contrast.py   cvd.py        │
    │           │           │
    └─────┬─────┘           │
          ▼                 │
      scoring.py            │
          │                 │
          ▼                 │
      palettes.py ──────────┘
```

No circular imports. Every module imports down, never up.

---

## Module Contracts

### `_types.py` — Type Definitions

Pure dataclasses and enums. No logic. No imports beyond stdlib.

```python
from dataclasses import dataclass
from enum import Enum

Color = str | tuple[int, int, int]  # "#1B4F72" or (27, 79, 114)

class WcagLevel(Enum):
    AA = "AA"
    AAA = "AAA"

class TextSize(Enum):
    NORMAL = "normal"    # < 18pt (or < 14pt bold)
    LARGE = "large"      # >= 18pt (or >= 14pt bold)

class CvdType(Enum):
    PROTANOPIA = "protanopia"        # No red cones
    DEUTERANOPIA = "deuteranopia"    # No green cones
    TRITANOPIA = "tritanopia"        # No blue cones

@dataclass(frozen=True)
class ContrastResult:
    ratio: float
    meets_aa_normal: bool
    meets_aa_large: bool
    meets_aaa_normal: bool
    meets_aaa_large: bool

@dataclass(frozen=True)
class PaletteScore:
    overall_score: float          # 0.0–1.0
    min_pairwise_contrast: float  # Lowest contrast ratio between any two colors
    mean_pairwise_contrast: float
    cvd_safe_count: int           # How many CVD types preserve distinguishability
    cvd_safe_types: list[CvdType]
    n_colors: int
    warnings: list[str]

@dataclass(frozen=True)
class PaletteInfo:
    name: str
    category: str                 # "qualitative", "sequential", "diverging"
    colors: tuple[str, ...]       # Hex colors
    min_n: int
    max_n: int
    accessibility_score: float
    cvd_safe: bool
```

---

### `color_utils.py` — Color Parsing & Conversion

Accepts hex strings (`"#1B4F72"`, `"#1B4"`, `"1B4F72"`) and RGB tuples. Normalizes everything to `(int, int, int)` internally, hex strings externally.

**Public functions:**

```python
def hex_to_rgb(hex_color: str) -> tuple[int, int, int]:
    """Parse hex string to RGB tuple. Accepts '#1B4F72', '1B4F72', '#1B4'."""

def rgb_to_hex(r: int, g: int, b: int) -> str:
    """Convert RGB tuple to hex string. Returns '#1B4F72' format."""

def normalize_color(color: Color) -> tuple[int, int, int]:
    """Accept hex or RGB, return RGB tuple. Raises ValueError on invalid input."""

def rgb_to_hsl(r: int, g: int, b: int) -> tuple[float, float, float]:
    """Convert RGB to HSL. Returns (hue 0-360, saturation 0-1, lightness 0-1)."""

def hsl_to_rgb(h: float, s: float, l: float) -> tuple[int, int, int]:
    """Convert HSL to RGB."""

def is_valid_hex(hex_color: str) -> bool:
    """Check if string is a valid hex color."""
```

**Validation rules:**
- Hex must be 3 or 6 hex digits, with optional `#` prefix
- RGB channels must be 0–255 integers
- Raise `ValueError` with descriptive message on invalid input

---

### `contrast.py` — WCAG 2.1 Contrast Ratio

Implements the WCAG 2.1 relative luminance and contrast ratio algorithms exactly as specified in the W3C recommendation.

**Algorithm — Relative Luminance (WCAG 2.1 §1.4.3):**

```
For each sRGB channel (R, G, B) in range [0, 255]:
    1. Normalize to [0, 1]: sRGB = channel / 255
    2. Linearize:
       if sRGB <= 0.04045:
           linear = sRGB / 12.92
       else:
           linear = ((sRGB + 0.055) / 1.055) ^ 2.4
    3. L = 0.2126 * R_linear + 0.7152 * G_linear + 0.0722 * B_linear
```

**Algorithm — Contrast Ratio:**

```
ratio = (L_lighter + 0.05) / (L_darker + 0.05)
```

Where `L_lighter` is the higher luminance value. Result is in range [1, 21].

**WCAG thresholds:**

| Level | Normal text | Large text |
|-------|------------|------------|
| AA    | >= 4.5     | >= 3.0     |
| AAA   | >= 7.0     | >= 4.5     |

**Public functions:**

```python
def relative_luminance(color: Color) -> float:
    """WCAG 2.1 relative luminance of a color. Returns 0.0 (black) to 1.0 (white)."""

def contrast_ratio(fg: Color, bg: Color) -> float:
    """WCAG 2.1 contrast ratio between two colors. Returns 1.0 to 21.0."""

def check_wcag(fg: Color, bg: Color, level: WcagLevel = WcagLevel.AA,
               size: TextSize = TextSize.NORMAL) -> bool:
    """Check if two colors meet a specific WCAG contrast threshold."""

def full_check(fg: Color, bg: Color) -> ContrastResult:
    """Run all WCAG checks and return a ContrastResult with all thresholds."""
```

**Implementation notes:**
- Use `math.pow()` for the gamma expansion, not `**` operator (consistent with W3C reference)
- The 0.04045 threshold is the EXACT W3C value — some implementations use 0.03928 (wrong, that's from an older draft)
- Always use the CORRECT threshold: `0.04045`

---

### `cvd.py` — Color Vision Deficiency Simulation

Simulates how colors appear to people with protanopia, deuteranopia, and tritanopia using the Brettel, Viénot & Mollon (1997) algorithm, refined by Viénot, Brettel & Mollon (1999).

**Algorithm — CVD Simulation:**

```
1. Convert sRGB to linear RGB (same linearization as contrast.py)
2. Apply 3×3 simulation matrix for the CVD type:
   [R']   [m00 m01 m02] [R]
   [G'] = [m10 m11 m12] [G]
   [B']   [m20 m21 m22] [B]
3. Clamp to [0, 1]
4. Convert back to sRGB (apply gamma compression)
5. Round to integer [0, 255]
```

**Simulation matrices (Viénot et al. 1999):**

```python
# Protanopia (no L-cones / red-blind)
PROTANOPIA_MATRIX = [
    [0.152286, 1.052583, -0.204868],
    [0.114503, 0.786281,  0.099216],
    [-0.003882, -0.048116, 1.051998],
]

# Deuteranopia (no M-cones / green-blind)
DEUTERANOPIA_MATRIX = [
    [0.367322, 0.860646, -0.227968],
    [0.280085, 0.672501,  0.047413],
    [-0.011820, 0.042940, 0.968881],
]

# Tritanopia (no S-cones / blue-blind)
TRITANOPIA_MATRIX = [
    [1.255528, -0.076749, -0.178779],
    [-0.078411, 0.930809, 0.147602],
    [0.004733, 0.691367, 0.303900],
]
```

**Public functions:**

```python
def simulate_cvd(color: Color, cvd_type: CvdType) -> str:
    """Simulate how a color appears with a given CVD type. Returns hex string."""

def simulate_palette(colors: list[Color], cvd_type: CvdType) -> list[str]:
    """Simulate an entire palette under a CVD type. Returns list of hex strings."""

def is_distinguishable(color1: Color, color2: Color, cvd_type: CvdType,
                        min_delta_e: float = 10.0) -> bool:
    """Check if two colors remain distinguishable under a CVD type.
    Uses simplified Euclidean distance in linear RGB space (not full CIEDE2000,
    to avoid numpy/scipy dependency). Threshold is tuned for cartographic use."""
```

**Implementation notes:**
- Matrices are for COMPLETE dichromacy (100% severity). Anomalous trichromacy (partial) is out of scope for v1.
- The `is_distinguishable` function uses Euclidean distance in linear RGB, not perceptual CIEDE2000. This is a deliberate simplification — see `ai-dev/decisions/DL-001-zero-dependencies.md`. The threshold (default 10.0) is tuned conservatively for cartographic use where colors appear as map fills, not small text.
- Gamma compression (linear → sRGB) is the inverse of the linearization in `contrast.py`. Factor this into a shared private function in `color_utils.py`.

---

### `scoring.py` — Palette Accessibility Scoring

Scores an entire color palette on a 0.0–1.0 scale based on pairwise contrast ratios and CVD distinguishability.

**Scoring algorithm:**

```
1. Compute all pairwise contrast ratios (n*(n-1)/2 pairs)
2. min_contrast = min(all pairwise ratios)
3. mean_contrast = mean(all pairwise ratios)
4. For each CVD type (protanopia, deuteranopia, tritanopia):
   - Simulate all colors under that CVD type
   - Check all pairwise distinguishability
   - If ALL pairs are distinguishable → CVD type is "safe"
5. Score components:
   - contrast_score = clamp(min_contrast / 4.5, 0, 1)  # 4.5 = AA normal threshold
   - cvd_score = cvd_safe_count / 3  # 3 CVD types
   - overall_score = 0.6 * contrast_score + 0.4 * cvd_score
6. Warnings:
   - min_contrast < 3.0 → "Palette has very low contrast pairs"
   - min_contrast < 4.5 → "Some pairs fail WCAG AA for normal text"
   - cvd_safe_count == 0 → "Palette is not safe for any color vision deficiency"
```

**Public functions:**

```python
def score_palette(colors: list[Color]) -> PaletteScore:
    """Score a palette's overall accessibility. Returns PaletteScore dataclass."""

def pairwise_contrasts(colors: list[Color]) -> list[tuple[int, int, float]]:
    """Compute all pairwise contrast ratios. Returns list of (i, j, ratio) tuples."""

def check_palette_cvd(colors: list[Color],
                       cvd_types: list[CvdType] | None = None,
                       min_delta_e: float = 10.0) -> dict[CvdType, bool]:
    """Check palette distinguishability under each CVD type.
    Returns {CvdType: all_pairs_distinguishable}."""
```

---

### `palettes.py` — Pre-Built Accessible Palettes

Ships a curated set of accessible color palettes for cartographic use. Every palette is verified at import time via `scoring.py` (development) or at build time (production).

**Palette categories:**

| Category | Use case | Example names |
|----------|---------|---------------|
| `qualitative` | Categorical maps (land use, permit types) | `qualitative_6`, `qualitative_8` |
| `sequential` | Continuous data (elevation, population density) | `blues`, `greens`, `reds`, `purples` |
| `diverging` | Deviation from center (change detection, anomaly) | `red_blue`, `brown_teal` |

**Public functions:**

```python
def get_palette(name: str, n: int | None = None) -> list[str]:
    """Get a palette by name. Optional n to subsample. Returns list of hex strings."""

def list_palettes(category: str | None = None) -> list[PaletteInfo]:
    """List all available palettes, optionally filtered by category."""

def qualitative(n: int) -> list[str]:
    """Get a qualitative (categorical) palette with n colors. Convenience shortcut."""

def sequential(name: str, n: int = 7) -> list[str]:
    """Get a sequential palette by name with n steps. Convenience shortcut."""

def diverging(name: str, n: int = 7) -> list[str]:
    """Get a diverging palette by name with n steps. Convenience shortcut."""
```

**Palette storage:**
Palettes are defined as a Python dict in `palettes.py` (no external JSON file for the Python version — keep it zero-dependency). The R and .NET versions share a `palettes.json` that is generated FROM the Python definitions.

---

## Shared Test Vectors

`tests/vectors/test_colors.json` is the cross-language contract. Format:

```json
{
  "contrast_ratio": [
    {
      "fg": "#1B4F72",
      "bg": "#FFFFFF",
      "expected_ratio": 10.53,
      "expected_luminance_fg": 0.0468,
      "expected_luminance_bg": 1.0,
      "meets_aa_normal": true,
      "meets_aa_large": true,
      "meets_aaa_normal": true,
      "meets_aaa_large": true
    }
  ],
  "cvd_simulation": [
    {
      "input": "#FF0000",
      "protanopia": "#9F8700",
      "deuteranopia": "#A58400",
      "tritanopia": "#FF1500"
    }
  ],
  "palette_scoring": [
    {
      "colors": ["#1B4F72", "#F39C12", "#27AE60", "#E74C3C", "#8E44AD"],
      "expected_min_contrast": 1.42,
      "expected_cvd_safe_types": ["tritanopia"]
    }
  ]
}
```

This file is copied verbatim into the R (`inst/testdata/test_colors.json`) and .NET (`tests/TestVectors/test_colors.json`) packages. If the Python test suite passes with these vectors, the R and .NET suites must also pass with the same vectors and tolerances.

**Tolerance:** All floating-point comparisons use `±0.01` for contrast ratios and luminance values, `±2` for RGB channel values (rounding differences in CVD simulation).

---

## Error Handling Strategy

All public functions raise `ValueError` for invalid input (bad hex, out-of-range RGB, unknown palette name). No custom exception types in v1 — `ValueError` with descriptive messages is sufficient for a utility library.

```python
# Pattern for input validation
def contrast_ratio(fg: Color, bg: Color) -> float:
    fg_rgb = normalize_color(fg)  # Raises ValueError if invalid
    bg_rgb = normalize_color(bg)  # Raises ValueError if invalid
    # ... compute ...
```

No logging. No warnings module. No stdout. This is a pure computation library.
