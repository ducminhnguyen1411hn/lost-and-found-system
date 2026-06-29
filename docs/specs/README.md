# Specs — source design documents

The five documents in this folder are the **authoritative product/engineering specs** for
LostAndFound. They are **faithful English translations** of the original Vietnamese source files
(the prose was translated; code identifiers, enum values, `FR-*` ids, tables, and Mermaid diagrams
are preserved verbatim).

| File | What it covers |
|---|---|
| [PRODUCT_OVERVIEW.md](PRODUCT_OVERVIEW.md) | Context, pain points, solution overview, scope, goals — read first. |
| [PROJECT_INSTRUCTION.md](PROJECT_INSTRUCTION.md) | Architecture, data model, state machines, tags, blind listing, conventions. |
| [REQUIREMENTS_2DEV.md](REQUIREMENTS_2DEV.md) | Functional/non-functional requirements (`FR-*` / `NFR-*`) and the 2-developer work split. |
| [DIAGRAMS.md](DIAGRAMS.md) | Mermaid state machines (FoundItem, Claim) and the ERD. |
| [FEATURE_PLAYBOOK.md](FEATURE_PLAYBOOK.md) | How to write a Feature Record; vertical-slice anatomy; worked example. |

> The condensed, always-current index is [../INDEX.md](../INDEX.md). When a spec conflicts with
> **[../../CLAUDE.md](../../CLAUDE.md)**, CLAUDE.md wins — notably **DB-First** overrides the
> "Code First + Migrations" wording carried over from the original `PROJECT_INSTRUCTION`.
