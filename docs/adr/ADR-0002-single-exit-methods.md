# ADR-0002: Single-Exit Methods

## Status

Accepted.

## Context

The project requires predictable method flow, explicit final results, and a consistent coding style for human and AI contributors.

## Decision

Every non-void method must contain exactly one return statement.

Early returns are forbidden.

Validation failures use typed exceptions or explicit validation results.

Complex branching must be reduced through cohesive private methods instead of deeply nested control flow.

## Consequences

Positive:

- Predictable result construction.
- Uniform style.
- Easier AI rule enforcement.
- A single observable return point.

Negative:

- Some methods may require a result variable.
- Guard-clause style is not used.
- Poor implementations may create unnecessary nesting.

## Guardrail

When the single-exit rule causes deep nesting, extract private methods or redesign the responsibility. Do not preserve a large method only to keep one return.
