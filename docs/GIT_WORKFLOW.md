# Git Workflow

OpenStore uses feature branches and small commits.

The goal is to keep each module or feature isolated, reviewable, and easy to integrate.

---

## 1. Branch strategy

Create one branch per feature or module-level change.

Branch name format:

```text
feature/<bounded-context>-<feature-name>
```

Examples:

```text
feature/tenancy-create-tenant
feature/catalog-create-product
feature/stores-publish-store
feature/public-whatsapp-cart
```

Use `fix/<bounded-context>-<bug-name>` for bug fixes and `chore/<topic>` for non-feature maintenance.

Do not mix unrelated modules in one branch.

---

## 2. What belongs in a feature branch

A feature branch should contain everything needed for that feature:

- Domain model changes.
- Application service or use case.
- API endpoint.
- Infrastructure persistence and migrations.
- Unit tests.
- Integration tests.
- Postman requests.
- Documentation updates.
- `.ai/CONTEXT_MAP.md` updates.

If models are created for a feature, commit them on the same feature branch. Do not create model commits on a separate unrelated branch unless the model is intentionally shared foundation work approved by the user.

---

## 3. Commit strategy

Use small, coherent commits.

Prefer commits that represent one logical step:

```text
Add tenant domain model
Add create tenant application service
Add create tenant endpoint
Add tenant persistence configuration
Add create tenant tests
Add create tenant Postman requests
Update context map for tenancy feature
```

Avoid one large commit containing the entire feature unless the change is very small.

Avoid commits that mix unrelated concerns, such as catalog changes and tenancy changes.

---

## 4. Commit message style

Use clear English imperative messages.

Good:

```text
Add tenant domain model
Add create tenant endpoint
Add Postman requests for tenant creation
```

Avoid:

```text
changes
fix stuff
wip
tenant and product and postman
```

Temporary WIP commits are allowed locally only when the user explicitly wants them. Clean up or squash them before integration when possible.

---

## 5. Agent behavior

Before editing code, an AI agent should:

1. Check current branch and working tree status.
2. Confirm whether the current branch matches the requested feature.
3. Create or suggest a feature branch when work starts from a base branch.
4. Avoid changing unrelated files.
5. Keep commits small and grouped by functionality when the user asks the agent to commit.
6. Never push unless the user explicitly asks.

If the working tree contains unrelated changes, the agent must not revert them. It should work around them or ask when they block the task.

---

## 6. Suggested feature flow

```text
git status
git switch -c feature/tenancy-create-tenant

# implement domain model
git add <domain files>
git commit -m "Add tenant domain model"

# implement use case
git add <application files>
git commit -m "Add create tenant application service"

# implement endpoint
git add <api files>
git commit -m "Add create tenant endpoint"

# add tests
git add <test files>
git commit -m "Add create tenant tests"

# add Postman docs
git add postman/
git commit -m "Add create tenant Postman requests"

# update context map/docs
git add .ai/CONTEXT_MAP.md docs/
git commit -m "Update context map for tenant creation"
```

The exact commit split may change, but each commit must remain coherent.

---

## 7. Pull request readiness

Before pushing or opening a PR:

- Build passes.
- Unit tests pass.
- Integration tests pass only when the feature explicitly added or required them.
- Postman requests are updated for changed endpoints.
- `.ai/CONTEXT_MAP.md` is current.
- No unrelated files are included.
- Commit history is understandable.

