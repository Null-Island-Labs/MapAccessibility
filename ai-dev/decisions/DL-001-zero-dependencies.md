# DL-001: Zero External Dependencies

**Date:** 2026-03-08
**Status:** Accepted
**Author:** Chris Lyons

## Context

`map-accessibility` implements WCAG contrast ratios, CVD simulation, and palette scoring. These are well-defined algorithms that could benefit from numpy (matrix multiplication, vectorized operations) or colormath (perceptual color differences). The question is whether to accept external dependencies.

## Decision

The package has zero external dependencies. All algorithms are implemented using Python stdlib only (`math`, `colorsys`, `dataclasses`, `enum`, `json`).

## Alternatives Considered

- **numpy** for CVD matrix multiplication — Rejected because a 3×3 × 3×1 matrix multiply is 9 multiplications and 3 additions. numpy would add a 20MB+ dependency for 12 lines of code.
- **colormath** for CIEDE2000 perceptual color difference — Rejected because it pulls in numpy and networkx transitively. CIEDE2000 is out of scope for v1; Euclidean distance in linear RGB is sufficient for palette-level distinguishability.
- **colour-science** for comprehensive color science — Rejected because it's a massive dependency (scipy, numpy, imageio). Overkill for WCAG contrast ratios.

## Consequences

- **Enables:** Embedding anywhere (Lambda, WASM, Jupyter, constrained environments). Trivial install. Fast CI. No dependency conflicts.
- **Constrains:** No CIEDE2000 perceptual color difference in v1. CVD distinguishability uses Euclidean distance in linear RGB, which is less perceptually accurate but adequate for cartographic palette validation. v2 may add optional numpy acceleration behind a `map-accessibility[fast]` extra.
