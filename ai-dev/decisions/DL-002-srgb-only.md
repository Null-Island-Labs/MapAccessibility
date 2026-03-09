# DL-002: sRGB Color Space Only

**Date:** 2026-03-08
**Status:** Accepted
**Author:** Chris Lyons

## Context

Color accessibility checking can be performed in various color spaces: sRGB, Display P3, Adobe RGB, CIELAB, CIELUV, etc. WCAG 2.1 defines contrast ratios in terms of sRGB relative luminance. CVD simulation matrices are defined for sRGB. The question is whether to support other color spaces.

## Decision

The package only supports sRGB. All inputs are assumed to be sRGB. No color space conversion is provided.

## Alternatives Considered

- **Support Display P3** — Rejected. Display P3 is increasingly common on Apple devices, but WCAG 2.1 is defined in sRGB. Most web and GIS content is sRGB. Supporting P3 would require gamut mapping and add complexity without serving the primary use case.
- **Support CIELAB for perceptual differences** — Rejected for v1. CIELAB requires XYZ conversion and illuminant specification. This is scope creep for a WCAG-focused tool. May be added in v2 for CIEDE2000 support.

## Consequences

- **Enables:** Simpler API (no color space parameter on every function), smaller codebase, exact match with WCAG 2.1 spec.
- **Constrains:** Colors from wide-gamut workflows must be converted to sRGB before checking. This is documented in the README and docstrings.
