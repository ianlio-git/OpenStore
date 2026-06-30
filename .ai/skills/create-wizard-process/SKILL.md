---
name: create-wizard-process
description: Implement or modify an OpenStore wizard, resumable workflow, state machine, multi-step onboarding, invitation flow, order preparation flow, delivery flow, approval flow, or other tenant-aware process with explicit states, transitions, idempotency, persistence, authorization, typed exceptions, compensation, and transition tests.
---

# Create Wizard or Multi-Step Process

## Objective

Implement a tenant-aware, resumable, testable multi-step business process.

## Required inputs

- Process name.
- Initial state.
- States.
- Commands or steps.
- Valid transitions.
- Invalid transitions.
- Completion conditions.
- Cancellation conditions.
- Persistence requirements.
- External dependencies.
- Required permissions.

Ask for clarification when states or valid transitions are unknown; do not invent business process rules.

## Steps

1. Identify the owning bounded context, tenant boundary, store boundary, and required permissions.
2. Define the process state enum or value object.
3. Define the transition table before implementation.
4. Define process commands and results.
5. Define typed invalid-transition exceptions and missing-resource exceptions.
6. Define the process service or state machine.
7. Inject `IDateTimeProvider`.
8. Add tenant, store, and authorization checks before each transition.
9. Add idempotency rules for repeated commands.
10. Persist state when resume or auditability is required.
11. Add compensation rules when external side effects require them.
12. Keep state changes in one explicit place.
13. Add unit tests for every valid transition.
14. Add tests for every invalid transition.
15. Add resume, repeat, cancellation, and completion tests.
16. Add cross-tenant tests.
17. Add unit tests for persisted-state decisions through abstractions. Ask before adding full persistence integration tests.
18. Run formatting, build, and unit tests. Run integration tests only when explicitly requested.

## Code rules

- One return statement.
- No early returns.
- State changes occur in one explicit place.
- External calls are behind interfaces.
- Time is deterministic.
- Exceptions are typed.
- Methods are ordered private, protected, public.

## Transition table format

Document the transition table in code comments, tests, or documentation when the workflow is not obvious:

```text
CurrentState -> Command -> NextState
Draft -> Submit -> Submitted
Submitted -> Approve -> Approved
Submitted -> Reject -> Rejected
```

## Completion report

Report states, transitions, tenant/store protections, authorization checks, idempotency behavior, persistence, tests, commands, and remaining risks.

## Definition of done

Every state and transition is explicit, authorized, tenant-safe, deterministic, resumable when required, and covered by tests.
