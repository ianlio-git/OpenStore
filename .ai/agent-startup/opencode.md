# OpenCode Startup

Use this routine before editing.

1. Read `OPENCODE.md`.
2. Read `.ai/CONTEXT_MAP.md`.
3. Read `AGENTS.md`.
4. Read `.ai/README.md`.
5. Read the applicable `.ai/skills/<skill-name>/SKILL.md`.
6. Read only task-relevant docs, source files, and tests.
7. Treat skill files as plain Markdown workflows. Native skill support is not required.
8. Use `.ai/CONTEXT_MAP.md` as the routing index to avoid unnecessary file loading.
9. Follow architecture, infrastructure, coding, testing, exception, reuse, and tenant-isolation rules.
10. Update `.ai/CONTEXT_MAP.md` when structure, entry points, standard commands, or important decisions change.
11. Do not claim completion until applicable build and tests pass, unless explicitly blocked.

Short invocation:

```text
Follow .ai/agent-startup/opencode.md.
Use <skill-name> to <task>.
```
