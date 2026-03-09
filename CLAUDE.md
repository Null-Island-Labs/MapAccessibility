# CLAUDE.md — map-accessibility
> WCAG 2.1 AA color accessibility checking for cartographic palettes
> Python · Pure stdlib · Zero dependencies · PyPI package

Read this file completely before doing anything.
Then read `ai-dev/architecture.md` for full context.
Then read `ai-dev/guardrails/` for hard constraints.

---

## Workflow Protocol

When starting a new task:
1. Read CLAUDE.md (this file)
2. Read ai-dev/architecture.md
3. Read ai-dev/guardrails/ for constraints that override all other guidance
4. Read the relevant ai-dev/agents/ file for your role
5. Check ai-dev/decisions/ for prior decisions that may affect your work
6. Check ai-dev/skills/ for domain patterns specific to this project

Before writing code:
1. Confirm you understand the module's responsibility
2. List the files you will create or modify
3. Show the plan

Do not proceed until confirmed.

---

## Compatibility Matrix

| Component | Version |
|---|---|
| Python | >= 3.9 |
| Dependencies | **None** — stdlib only |
| Test framework | pytest >= 7.0 |
| Linter | ruff |
| Type checker | mypy (strict mode) |
| Formatter | ruff format |

---

## Project Structure

```
map-accessibility/
├── CLAUDE.md                        # You are here
├── README.md                        # PyPI/GitHub readme
├── LICENSE                          # MIT
├── pyproject.toml                   # Build config (hatchling)
├── ai-dev/                          # AI development infrastructure
│   ├── architecture.md              # Module design, data flow, algorithms
│   ├── spec.md                      # Requirements, acceptance criteria
│   ├── patterns.md                  # Code patterns and anti-patterns
│   ├── agents/                      # Specialized agent configs
│   │   ├── README.md
│   │   ├── architect.md
│   │   ├── python_expert.md
│   │   ├── color_science_expert.md  # Domain-specific
│   │   └── qa_reviewer.md
│   ├── decisions/                   # Architectural decision records
│   │   ├── DL-001-zero-dependencies.md
│   │   ├── DL-002-srgb-only.md
│   │   └── DL-003-shared-test-vectors.md
│   ├── guardrails/
│   │   ├── coding-standards.md
│   │   └── cross-language-compliance.md
│   └── skills/
│       └── color-science-skill.md
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                   # Test on push/PR
│   │   └── publish.yml              # Publish to PyPI on tag
│   └── copilot-instructions.md
├── src/
│   └── map_accessibility/
│       ├── __init__.py              # Public API re-exports
│       ├── contrast.py              # WCAG contrast ratio + luminance
│       ├── cvd.py                   # Color vision deficiency simulation
│       ├── scoring.py               # Palette accessibility scoring
│       ├── palettes.py              # Pre-built accessible palettes
│       ├── color_utils.py           # Hex/RGB/HSL parsing and conversion
│       └── _types.py                # Type definitions (dataclasses)
├── tests/
│   ├── conftest.py                  # Shared fixtures, test vector loading
│   ├── test_contrast.py
│   ├── test_cvd.py
│   ├── test_scoring.py
│   ├── test_palettes.py
│   ├── test_color_utils.py
│   └── vectors/
│       └── test_colors.json         # Shared test vectors (Python/R/.NET)
└── assets/
    ├── banner.svg                   # README banner (1280×320)
    ├── banner.png
    ├── logo.svg                     # PyPI logo (512×512)
    └── logo.png
```

---

## Critical Conventions

### Import pattern
```python
# Public API — users import from the package root
from map_accessibility import contrast_ratio, check_wcag, simulate_cvd, score_palette

# Internal imports use relative paths
from .color_utils import hex_to_rgb, rgb_to_hex
from ._types import WcagLevel, TextSize, CvdType, PaletteScore
```

### Function signatures
All public functions accept colors as hex strings (`"#1B4F72"`) or RGB tuples (`(27, 79, 114)`). Every public function has a `-> TypeAnnotation` return type. No `Any` types in the public API.

### Naming
- Functions: `snake_case` — `contrast_ratio()`, `check_wcag()`, `simulate_cvd()`
- Types/dataclasses: `PascalCase` — `PaletteScore`, `WcagLevel`, `CvdType`
- Private helpers: `_prefixed` — `_linearize_channel()`, `_apply_cvd_matrix()`
- Module files: `snake_case.py` — `color_utils.py`, `contrast.py`

### No dependencies
This package has ZERO external dependencies. Do NOT add any. All color math uses stdlib `math` and `colorsys`. All types use stdlib `dataclasses` and `enum`. This is a hard constraint — see `ai-dev/guardrails/coding-standards.md` and `ai-dev/decisions/DL-001-zero-dependencies.md`.

---

## Architecture Summary

Five modules, each with a single responsibility:

1. **`contrast.py`** — WCAG 2.1 relative luminance and contrast ratio. The core math.
2. **`cvd.py`** — Color vision deficiency simulation using Brettel/Viénot matrices.
3. **`scoring.py`** — Palette-level accessibility scoring (min pairwise contrast, CVD safety, overall score).
4. **`palettes.py`** — Pre-built accessible palettes (qualitative, sequential, diverging).
5. **`color_utils.py`** — Hex/RGB/HSL parsing, conversion, and validation.

Data flows one direction: `color_utils` → `contrast` / `cvd` → `scoring`. No circular imports. `palettes` depends on `scoring` for self-verification.

Detailed design in `ai-dev/architecture.md`.

---

## What NOT To Do

- Do NOT add any external dependencies. Not even `numpy`. Not even `colormath`. Stdlib only.
- Do NOT support color spaces other than sRGB. See `ai-dev/decisions/DL-002-srgb-only.md`.
- Do NOT use `float` comparisons with `==`. Use `math.isclose()` for all floating-point equality.
- Do NOT hardcode test expected values inline. All test vectors live in `tests/vectors/test_colors.json`.
- Do NOT use `print()` for any output. This is a library — no stdout.
- Do NOT use `from __future__ import annotations` — it breaks runtime type inspection in some contexts. Use string annotations only where needed for forward references.
- Do NOT implement color naming (CSS named colors, X11 colors). That's scope creep. Accept hex and RGB only.
