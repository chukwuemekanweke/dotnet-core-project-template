---
type: workflow
title: Pull Request and Documentation Voice Conventions
description: PR template usage, title format, and the plain human writing voice expected in PR descriptions and documentation
resource: okf://knowledge/workflows/pull-requests
tags:
    - context
    - workflow
    - pull-requests
    - writing-voice
governance: context
code_refs:
    - .github/pull_request_template.md
sources:
    - kind: file
      path: AGENTS.md
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Pull requests

- Use `.github/pull_request_template.md` as the structure for any generated PR title/description. Return the description as Markdown, ready to paste directly into GitHub.
- When summarizing a branch, review all commits on it and reflect the full scope of work — don't describe only the latest commit.
- Prefer a title like `feat(scope): summary of the change` that states the main change and affected area.

## Writing voice (PR descriptions and docs)

Write like a maintainer explaining the change to another engineer: direct, concrete, grounded in what actually changed. Avoid generic filler, marketing language, exaggerated claims, repetitive
summaries, and anything that reads as automated/generated. Keep the important technical tradeoffs; don't inflate a simple change into elaborate prose.
