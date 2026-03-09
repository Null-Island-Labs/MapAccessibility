# Cross-Language Compliance Guardrails — map-accessibility

This package is the **reference implementation** for the Null Island Labs
accessibility stack. The R (`mapaccessibility`) and .NET (`MapAccessibility`)
packages must produce identical results. These rules enforce that contract.

---

## Shared Test Vectors

- `tests/vectors/test_colors.json` is the source of truth
- Every algorithm change MUST update the test vectors
- Test vectors are copied verbatim to R and .NET repos — they are not adapted
- If a new test case is added in Python, it must be added to the JSON file, not just as an inline pytest case

## Tolerances

These tolerances are the contract. All three languages must agree within these bounds.

| Metric | Tolerance | Notes |
|--------|-----------|-------|
| Relative luminance | ±0.0001 | 4 decimal places |
| Contrast ratio | ±0.01 | 2 decimal places |
| CVD simulated RGB channel | ±2 | Integer rounding differences |
| Palette overall score | ±0.05 | Compound rounding |

## Algorithm Fidelity

- The sRGB linearization threshold is `0.04045` in ALL three languages
- The gamma expansion exponent is `2.4` in ALL three languages
- The luminance coefficients are `0.2126, 0.7152, 0.0722` in ALL three languages
- The CVD simulation matrices are identical across ALL three languages (copy from architecture.md)
- The palette scoring weights are `0.6 * contrast + 0.4 * cvd` in ALL three languages

## What Changes Require Cross-Language Updates

| Change | Update Required |
|--------|----------------|
| New palette added | Update palettes in R and .NET |
| Algorithm bug fix | Fix in ALL three languages + update test vectors |
| New CVD type added | Add to ALL three languages |
| Scoring formula change | Change in ALL three languages + update test vectors |
| New public function | Implement in ALL three languages (or document as language-specific) |
| Threshold change | Change in ALL three languages |

## What Does NOT Require Cross-Language Updates

- Internal refactoring (private helper reorganization)
- Language-specific integrations (ggplot2 scales in R, SkiaSharp bridge in .NET)
- Performance optimizations that don't change output
- Additional convenience functions that wrap existing public API
