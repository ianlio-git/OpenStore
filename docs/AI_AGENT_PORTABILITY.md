# AI Agent Portability

OpenStore AI rules are tool-agnostic.

Codex, OpenCode, Antigravity CLI, or any other coding agent should follow the same project rules by reading the same source files:

1. `.ai/CONTEXT_MAP.md`
2. `AGENTS.md`
3. `.ai/README.md`
4. The applicable `.ai/skills/**/SKILL.md`
5. Task-relevant files from `docs/`
6. Target source and test files

The `.ai/skills/` folder is written as plain Markdown on purpose. A tool does not need native "skills" support. If the tool cannot auto-discover skills, paste the skill name in the prompt and tell the agent to read the matching `SKILL.md`.

---

## 1. Common startup prompt

For day-to-day usage, prefer the short startup files in `.ai/agent-startup/`.

```text
Follow .ai/agent-startup/codex.md.
Use create-feature-slice to implement CreateTenant.
```

Available routines:

| Tool | Startup file |
|---|---|
| Codex | `.ai/agent-startup/codex.md` |
| OpenCode | `.ai/agent-startup/opencode.md` |
| Antigravity CLI | `.ai/agent-startup/antigravity.md` |
| Any other agent | `.ai/agent-startup/generic.md` |

If a CLI cannot read files automatically, paste the content of the matching startup file once at session start.

---

## 2. Common full startup prompt

Use this prompt in any agent:

```text
Before editing, read .ai/CONTEXT_MAP.md, AGENTS.md, .ai/README.md, and the applicable .ai/skills/**/SKILL.md.
Use the context map as a routing index to avoid loading unnecessary files.
Follow the OpenStore architecture, coding, infrastructure, testing, exception, reuse, and tenant-isolation rules.
Update .ai/CONTEXT_MAP.md if structure, entry points, standard commands, or important decisions change.
```

---

## 3. Codex usage

Codex should naturally read `AGENTS.md` and can use the `.ai/skills/` files when named in the prompt.

Example:

```text
Use create-feature-slice to implement CreateTenant.
Read .ai/CONTEXT_MAP.md first, then AGENTS.md, .ai/README.md, and .ai/skills/create-feature-slice/SKILL.md.
Create only the needed src and tests structure.
Update .ai/CONTEXT_MAP.md if structure or commands change.
```

---

## 4. OpenCode usage

If OpenCode does not automatically load this repository's instructions, start the session with:

```text
Use this repository's AI rules.
Read AGENTS.md, OPENCODE.md, .ai/CONTEXT_MAP.md, .ai/README.md, and the applicable .ai/skills/**/SKILL.md before editing.
Treat .ai/skills/<name>/SKILL.md as the workflow for the requested task.
```

Then ask for the specific work:

```text
Use create-feature-slice to implement CreateTenant in the Tenancy bounded context.
```

---

## 5. Antigravity CLI usage

If Antigravity CLI does not automatically load this repository's instructions, start the session with:

```text
Use this repository's AI rules.
Read AGENTS.md, ANTIGRAVITY.md, .ai/CONTEXT_MAP.md, .ai/README.md, and the applicable .ai/skills/**/SKILL.md before editing.
Treat .ai/skills/<name>/SKILL.md as the workflow for the requested task.
```

Then ask for the specific work:

```text
Use create-feature-slice to implement CreateTenant in the Tenancy bounded context.
```

---

## 6. Tool-neutral skill invocation

All agents should understand these natural-language invocations:

```text
Use create-feature-slice to ...
Use create-crud-use-case to ...
Use add-tenant-entity to ...
Use write-tests to ...
Use create-external-service-client to ...
Use create-wizard-process to ...
Use create-microservice to ...
Use update-context-map to ...
```

If a tool cannot resolve a skill name, explicitly include the path:

```text
Use the workflow in .ai/skills/create-feature-slice/SKILL.md to implement CreateTenant.
```

---

## 7. Maintenance rule

Do not fork rules per tool unless the tool requires a thin startup file.

The source of truth remains:

- `AGENTS.md` for mandatory agent behavior.
- `.ai/CONTEXT_MAP.md` for the compact codebase index.
- `.ai/README.md` for skill usage.
- `.ai/skills/**/SKILL.md` for workflows.
- `docs/` for project standards.
