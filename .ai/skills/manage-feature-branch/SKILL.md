---
name: manage-feature-branch
description: Plan or apply OpenStore Git workflow for feature branches, module-scoped work, small commits, branch naming, commit grouping, PR readiness, and safe handling of dirty working trees. Use when the user asks to create a branch, organize commits, commit feature work, prepare a PR, split work by module, or keep models and feature changes on the same branch.
---

# Manage Feature Branch

## Objective

Keep feature work isolated in one branch per feature or module-level change, with small coherent commits.

## Steps

1. Read `docs/GIT_WORKFLOW.md`.
2. Check current branch and working tree status.
3. Identify the requested bounded context and feature.
4. Choose a branch name using `feature/<bounded-context>-<feature-name>`.
5. If already on a suitable feature branch, continue there.
6. If on a base branch, create or suggest the feature branch.
7. Keep all code, models, tests, Postman requests, docs, and context-map updates for the feature on that branch.
8. Do not mix unrelated modules or features.
9. When committing, group changes into small coherent commits.
10. Never push unless the user explicitly asks.

## Commit grouping

Prefer commits like:

```text
Add tenant domain model
Add create tenant application service
Add create tenant endpoint
Add create tenant tests
Add create tenant Postman requests
Update context map for tenant creation
```

## Dirty working tree rules

- Do not revert user changes.
- Ignore unrelated changes when they do not block the task.
- Ask only when unrelated changes make the requested work unsafe or impossible.
- Do not include unrelated files in commits.

## Completion report

Report:

- Current branch.
- Branch created or reused.
- Commit plan or commits created.
- Files intentionally included.
- Unrelated changes left untouched.
- Whether push was skipped or requested.

