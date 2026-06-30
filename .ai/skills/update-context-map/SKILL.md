---
name: update-context-map
description: Update OpenStore .ai/CONTEXT_MAP.md after code, structure, command, architecture, infrastructure, testing, or skill changes. Use when the user asks for a codebase index, context map, Headroom-like summary, token-saving map, project overview, or when a task adds or moves important files, projects, entry points, standard commands, or decisions.
---

# Update Context Map

## Objective

Keep `.ai/CONTEXT_MAP.md` as a compact, current routing index that reduces repeated context loading.

The context map is not full documentation. It is a short navigation layer that points to the source of truth.

## Steps

1. Read `.ai/CONTEXT_MAP.md`.
2. Inspect current repository structure with a fast file listing.
3. Identify changes since the map was last accurate.
4. Update only the sections affected by the change.
5. Add compact pointers to important files, projects, commands, and entry points.
6. Remove stale pointers.
7. Keep summaries short.
8. Do not duplicate large content from docs or code.
9. Prefer source paths and one-line explanations.
10. Report what changed in the map.

## Add entries when

- A bounded context is added.
- A project is added, moved, renamed, or removed.
- A standard build, test, run, migration, deployment, or indexing command is established.
- A key API endpoint, service, module, adapter, process, or entry point is created.
- An ADR changes an architecture or infrastructure decision.
- A new reusable pattern appears.
- A token-saving pointer can replace repeated broad file reads.

## Do not add

- Full implementation details.
- Long copied sections from docs.
- Temporary local notes.
- Every file in the repository.
- Generated output.
- Secrets or environment-specific credentials.

## Preferred entry format

Use compact entries:

```text
Path: one-line reason to read it.
```

Example:

```text
src/Services/Tenancy/OpenStore.Tenancy.Application/Tenants/CreateTenantService.cs: CreateTenant use case and tenant owner assignment.
```

## Completion report

Report:

- Sections updated.
- Stale entries removed.
- New entry points or commands added.
- Anything intentionally left out to keep the map small.

