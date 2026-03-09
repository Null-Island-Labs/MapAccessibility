# Patterns — map-accessibility

> Code patterns, anti-patterns, and lessons learned

---

## Patterns

### Pattern: Color Input Normalization

Every public function that accepts a `Color` argument normalizes it immediately via `normalize_color()`. This happens once at the public API boundary — internal functions work with `tuple[int, int, int]` only.

```python
# ✅ CORRECT — normalize at the API boundary
def contrast_ratio(fg: Color, bg: Color) -> float:
    fg_rgb = normalize_color(fg)
    bg_rgb = normalize_color(bg)
    fg_lum = _relative_luminance_rgb(fg_rgb)
    bg_lum = _relative_luminance_rgb(bg_rgb)
    return _contrast_ratio_from_luminance(fg_lum, bg_lum)

# Internal helper works with tuples only — no normalization needed
def _relative_luminance_rgb(rgb: tuple[int, int, int]) -> float:
    r, g, b = rgb
    # ... compute ...
```

```python
# ❌ WRONG — normalizing deep in internal functions
def _some_internal_helper(color: Color) -> float:
    rgb = normalize_color(color)  # Don't do this — normalize at the boundary
```

---

### Pattern: Frozen Dataclass Results

All result types are `@dataclass(frozen=True)`. Immutable by default. No mutable state.

```python
# ✅ CORRECT
@dataclass(frozen=True)
class ContrastResult:
    ratio: float
    meets_aa_normal: bool
    meets_aa_large: bool
    meets_aaa_normal: bool
    meets_aaa_large: bool
```

```python
# ❌ WRONG — mutable dataclass
@dataclass
class ContrastResult:
    ratio: float
    meets_aa_normal: bool  # Someone could mutate this after creation
```

---

### Pattern: Private Helper Naming

Internal functions that are not part of the public API are prefixed with `_`. Module-level constants that are implementation details are prefixed with `_`.

```python
# ✅ CORRECT
_WCAG_AA_NORMAL_THRESHOLD = 4.5
_WCAG_AA_LARGE_THRESHOLD = 3.0

def _linearize_channel(channel: float) -> float:
    """sRGB to linear RGB for a single channel."""
    if channel <= 0.04045:
        return channel / 12.92
    return math.pow((channel + 0.055) / 1.055, 2.4)
```

---

### Pattern: Shared Linearization

The sRGB ↔ linear RGB conversion is needed by both `contrast.py` and `cvd.py`. Factor it into `color_utils.py` as private helpers that both modules import.

```python
# color_utils.py
def _srgb_to_linear(channel: float) -> float:
    """Convert a single sRGB channel [0, 1] to linear RGB [0, 1]."""
    if channel <= 0.04045:
        return channel / 12.92
    return math.pow((channel + 0.055) / 1.055, 2.4)

def _linear_to_srgb(channel: float) -> float:
    """Convert a single linear RGB channel [0, 1] to sRGB [0, 1]."""
    if channel <= 0.0031308:
        return channel * 12.92
    return 1.055 * math.pow(channel, 1.0 / 2.4) - 0.055
```

---

### Pattern: Matrix Multiplication Without numpy

CVD simulation requires 3×3 matrix × 3×1 vector multiplication. Implement it explicitly — it's 9 multiplications and 3 additions.

```python
# ✅ CORRECT — explicit, clear, no dependencies
def _apply_matrix(matrix: list[list[float]], rgb: tuple[float, float, float]) -> tuple[float, float, float]:
    r, g, b = rgb
    return (
        matrix[0][0] * r + matrix[0][1] * g + matrix[0][2] * b,
        matrix[1][0] * r + matrix[1][1] * g + matrix[1][2] * b,
        matrix[2][0] * r + matrix[2][1] * g + matrix[2][2] * b,
    )
```

```python
# ❌ WRONG — don't import numpy for 9 multiplications
import numpy as np
result = np.dot(matrix, rgb_vector)
```

---

### Pattern: Float Comparison

Never use `==` for floating-point comparison. Use `math.isclose()` with explicit tolerances.

```python
# ✅ CORRECT
import math

def test_contrast_ratio_black_white():
    ratio = contrast_ratio("#000000", "#FFFFFF")
    assert math.isclose(ratio, 21.0, rel_tol=1e-4)
```

```python
# ❌ WRONG
def test_contrast_ratio_black_white():
    assert contrast_ratio("#000000", "#FFFFFF") == 21.0
```

---

### Pattern: Test Vector Loading

Tests load shared vectors from JSON, not inline constants. The `conftest.py` provides a fixture.

```python
# conftest.py
import json
from pathlib import Path

import pytest

VECTORS_DIR = Path(__file__).parent / "vectors"

@pytest.fixture
def contrast_vectors():
    with open(VECTORS_DIR / "test_colors.json") as f:
        data = json.load(f)
    return data["contrast_ratio"]

# test_contrast.py
def test_contrast_matches_vectors(contrast_vectors):
    for v in contrast_vectors:
        ratio = contrast_ratio(v["fg"], v["bg"])
        assert math.isclose(ratio, v["expected_ratio"], abs_tol=0.01), \
            f"contrast_ratio({v['fg']}, {v['bg']}): expected {v['expected_ratio']}, got {ratio}"
```

---

## Anti-Patterns

### Anti-Pattern: Using 0.03928 Threshold

Some implementations use `0.03928` as the linearization threshold. This is from an older draft of the sRGB specification. The correct value per IEC 61966-2-1 and WCAG 2.1 is `0.04045`.

```python
# ❌ WRONG — old draft value
if channel <= 0.03928:
    return channel / 12.92

# ✅ CORRECT — current IEC 61966-2-1 / WCAG 2.1 value
if channel <= 0.04045:
    return channel / 12.92
```

This difference is small but real, and using the wrong value means our cross-language test vectors won't match.

---

### Anti-Pattern: Accepting Named Colors

Do not accept CSS named colors (`"red"`, `"steelblue"`) or X11 color names. This adds complexity (a 150+ entry lookup table) and ambiguity (X11 and CSS disagree on some names). Hex and RGB only.

```python
# ❌ WRONG — scope creep
def normalize_color(color):
    if isinstance(color, str) and color in CSS_NAMED_COLORS:
        return CSS_NAMED_COLORS[color]
    # ...

# ✅ CORRECT — hex and RGB only
def normalize_color(color: Color) -> tuple[int, int, int]:
    if isinstance(color, str):
        return hex_to_rgb(color)
    if isinstance(color, tuple) and len(color) == 3:
        # validate channels 0-255
        return color
    raise ValueError(f"Invalid color: {color!r}. Expected hex string or (R, G, B) tuple.")
```

---

### Anti-Pattern: Mutable Module-Level State

No global mutable state. Palettes are defined as frozen constants. No caching that could cause thread-safety issues.

```python
# ❌ WRONG — mutable module state
_palette_cache = {}

def get_palette(name):
    if name not in _palette_cache:
        _palette_cache[name] = _load_palette(name)
    return _palette_cache[name]

# ✅ CORRECT — immutable constant
_PALETTES: dict[str, PaletteInfo] = {
    "qualitative_6": PaletteInfo(
        name="qualitative_6",
        category="qualitative",
        colors=("#1B4F72", "#F39C12", "#27AE60", "#E74C3C", "#8E44AD", "#2C3E50"),
        min_n=3,
        max_n=6,
        accessibility_score=0.82,
        cvd_safe=True,
    ),
    # ...
}
```
