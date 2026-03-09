# Color Science Skill — map-accessibility

> Domain knowledge for WCAG color accessibility and CVD simulation

---

## Key References

| Document | What It Defines | URL |
|----------|----------------|-----|
| WCAG 2.1 §1.4.3 | Contrast ratio thresholds | https://www.w3.org/TR/WCAG21/#contrast-minimum |
| WCAG 2.1 Technique G18 | Relative luminance calculation | https://www.w3.org/WAI/WCAG21/Techniques/general/G18 |
| IEC 61966-2-1 | sRGB specification (linearization) | Standard document (not free) |
| Viénot, Brettel & Mollon (1999) | CVD simulation matrices | DOI: 10.1002/(SICI)1520-6378(199908)24:4<243::AID-COL5>3.0.CO;2-3 |
| Brettel, Viénot & Mollon (1997) | Original CVD simulation | DOI: 10.1364/JOSAA.14.002647 |

---

## WCAG 2.1 Relative Luminance — Step by Step

1. Start with sRGB values in [0, 255]
2. Normalize to [0, 1]: `sRGB = channel / 255.0`
3. Linearize (remove gamma encoding):
   - If `sRGB <= 0.04045`: `linear = sRGB / 12.92`
   - Else: `linear = ((sRGB + 0.055) / 1.055) ^ 2.4`
4. Weighted sum: `L = 0.2126 * R + 0.7152 * G + 0.0722 * B`

The weights come from the ITU-R BT.709 primaries (the same standard that defines sRGB).

**Common mistake:** Using `0.03928` instead of `0.04045`. The 0.03928 value appeared in an early draft of the sRGB spec. The final IEC 61966-2-1 standard and WCAG 2.1 both use `0.04045`. The difference is small (~0.001 in luminance) but matters for cross-language test vector agreement.

---

## Contrast Ratio Formula

```
ratio = (L1 + 0.05) / (L2 + 0.05)
```

Where L1 is the LIGHTER luminance (higher value) and L2 is the DARKER luminance. The +0.05 accounts for ambient light reflected off the screen. Result is always >= 1.0.

**Range:** 1.0 (identical colors) to 21.0 (black on white).

---

## WCAG Thresholds

| Level | Normal text | Large text |
|-------|------------|------------|
| AA    | >= 4.5:1   | >= 3.0:1   |
| AAA   | >= 7.0:1   | >= 4.5:1   |

"Large text" = >= 18pt, or >= 14pt bold.

For cartographic use: map fills are analogous to "large text" (large color areas). Map labels on fills should meet "normal text" thresholds.

---

## CVD Simulation — How It Works

Color vision deficiency (color blindness) occurs when one type of cone cell is missing or nonfunctional:

| Type | Missing Cone | Common Name | Prevalence |
|------|-------------|-------------|------------|
| Protanopia | L (long/red) | Red-blind | ~1% males |
| Deuteranopia | M (medium/green) | Green-blind | ~1% males |
| Tritanopia | S (short/blue) | Blue-blind | ~0.003% |

The Viénot/Brettel algorithm projects colors onto the reduced color gamut that the dichromat can perceive. This is done via a 3×3 matrix applied in linear RGB space:

1. Convert sRGB to linear RGB (same gamma removal as WCAG)
2. Apply the 3×3 simulation matrix
3. Clamp results to [0, 1]
4. Convert back to sRGB (apply gamma compression)

The matrices are NOT the same as daltonization matrices (which attempt to correct for CVD). Simulation shows what the person sees; daltonization adjusts colors to improve distinguishability.

---

## Palette Scoring — Design Rationale

The palette scoring formula (`0.6 * contrast + 0.4 * cvd`) was designed for cartographic use:

- **60% contrast weight** because low contrast is the most common accessibility failure in maps. Two similar blues are indistinguishable even for people with normal vision.
- **40% CVD weight** because CVD affects ~8% of males and ~0.5% of females. A palette that fails for deuteranopia alone affects ~5% of the population.

The contrast component uses `min_pairwise / 4.5` (clamped to [0,1]) because 4.5:1 is the WCAG AA threshold for normal text — a reasonable target for map legend readability.

The CVD component counts how many of the three CVD types preserve full distinguishability. 3/3 safe = 1.0, 2/3 = 0.67, 1/3 = 0.33, 0/3 = 0.0.

---

## Gamma Compression (Linear → sRGB)

The inverse of linearization, needed by CVD simulation to convert back to displayable sRGB:

```
if linear <= 0.0031308:
    sRGB = linear * 12.92
else:
    sRGB = 1.055 * linear^(1/2.4) - 0.055
```

Note: The threshold `0.0031308` is the exact inverse of `0.04045 / 12.92`. Some implementations compute it dynamically, but hardcoding it is fine and more readable.
