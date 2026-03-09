# DL-003: Shared Test Vectors as Cross-Language Contract

**Date:** 2026-03-08
**Status:** Accepted
**Author:** Chris Lyons

## Context

`map-accessibility` (Python), `mapaccessibility` (R), and `MapAccessibility` (.NET) must produce identical results. The question is how to enforce this contract across three codebases in three languages.

## Decision

A single `test_colors.json` file defines all test inputs and expected outputs. This file is copied verbatim into all three repos. All three test suites load this file and verify their implementations match.

## Alternatives Considered

- **Shared test runner** — Rejected. There's no practical way to run a single test suite across Python, R, and C#. Each language has its own test framework.
- **Golden file comparison** — Considered. Each implementation would generate output and diff against a golden file. Rejected because it adds a generation step and makes it unclear which implementation is "correct" when they diverge.
- **Spec-only contract** (algorithm description, no test data) — Rejected. Floating-point implementations diverge in practice even when the algorithm is "the same." Explicit expected values with tolerances are the only reliable contract.

## Consequences

- **Enables:** Any developer porting to a new language can validate their implementation against the same vectors. Regressions in any language are caught by the shared data. Adding new test cases in one language automatically creates a requirement in the others.
- **Constrains:** Algorithm changes require updating the JSON file first, then all three implementations. The Python implementation is the reference — if there's a disagreement, Python's output is correct and the others must adjust.
