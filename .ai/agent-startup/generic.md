# Generic Agent Startup

Use this routine for any AI coding agent.

1. Read `.ai/CONTEXT_MAP.md`.
2. Read `AGENTS.md`.
3. Read `.ai/README.md`.
4. Read the applicable `.ai/skills/<skill-name>/SKILL.md`.
5. Read only task-relevant docs, source files, and tests.
6. Treat skill files as plain Markdown workflows.
7. Use `.ai/CONTEXT_MAP.md` as the routing index to avoid unnecessary file loading.
8. Follow architecture, infrastructure, coding, testing, exception, reuse, and tenant-isolation rules.
9. Update `.ai/CONTEXT_MAP.md` when structure, entry points, standard commands, or important decisions change.
10. Do not claim completion until applicable build and tests pass, unless explicitly blocked.

Short invocation:

```text
Follow .ai/agent-startup/generic.md.
Use <skill-name> to <task>.
```
