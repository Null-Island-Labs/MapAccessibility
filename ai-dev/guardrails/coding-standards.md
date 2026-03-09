# Coding Standards Guardrails — map-accessibility

These rules apply to ALL code generated for this project, regardless
of which agent is active. Violations are treated as Critical findings.

---

## Python

- Target: Python >= 3.9
- Zero external dependencies. This is non-negotiable. stdlib only.
- All public functions must have full type annotations (parameters and return type)
- All public functions must have Google-style docstrings with Args/Returns/Raises sections
- `mypy --strict` must pass with zero errors
- `ruff check` must pass with zero errors
- `ruff format` must produce no changes (code is pre-formatted)
- No `print()` statements anywhere. This is a library.
- No `logging` module usage. This is a pure computation library.
- No `warnings` module usage. Raise `ValueError` or include warnings in result objects.
- No mutable default arguments
- No mutable module-level state
- All dataclasses use `frozen=True`
- All `float` equality uses `math.isclose()` with explicit tolerances
- Use `math.pow()` for the WCAG gamma expansion, not `**` operator
- Use the CORRECT sRGB linearization threshold: `0.04045` (not `0.03928`)

## Testing

- Framework: pytest >= 7.0
- All test expected values come from `tests/vectors/test_colors.json`
- No inline expected values for contrast ratios or CVD simulations
- Test tolerances: ±0.01 for contrast ratios, ±2 for RGB channels
- Tests must run offline (no network access)
- Tests must complete in < 10 seconds total
- 100% coverage of public API functions (not necessarily private helpers)

## Naming

- Functions: `snake_case`
- Types/classes: `PascalCase`
- Constants: `UPPER_SNAKE_CASE` (public) or `_UPPER_SNAKE_CASE` (private)
- Private helpers: `_prefixed`
- Module files: `snake_case.py`
- Test files: `test_{module_name}.py`

## File Organization

- One module per responsibility (contrast, cvd, scoring, palettes, color_utils, _types)
- Public API re-exported from `__init__.py`
- No circular imports — dependency flows downward (see architecture.md)
- Types live in `_types.py`, not scattered across modules
