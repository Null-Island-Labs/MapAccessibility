# ai-dev/agents — map-accessibility

## Available Agents

| Agent | File | Primary Use |
|---|---|---|
| Solutions Architect | `architect.md` | Module interfaces, API design, dependency graph |
| Python Expert | `python_expert.md` | Idiomatic Python, packaging, typing, stdlib patterns |
| Color Science Expert | `color_science_expert.md` | WCAG algorithms, CVD matrices, color math correctness |
| QA Reviewer | `qa_reviewer.md` | Test strategy, test vectors, edge cases, coverage |

## Agent Combination Matrix

| Phase | Primary | Supporting |
|---|---|---|
| API Design | Architect | Color Science Expert |
| Algorithm Implementation | Color Science Expert | Python Expert |
| Public API Implementation | Python Expert | Architect |
| Test Vector Generation | Color Science Expert | QA Reviewer |
| Test Suite Writing | QA Reviewer | Color Science Expert |
| Packaging & CI | Python Expert | — |
| Documentation | Python Expert | Color Science Expert |

## Usage

Reference from a Claude Code prompt:

```
Read CLAUDE.md, then ai-dev/agents/color_science_expert.md.
Implement the contrast.py module.
```
