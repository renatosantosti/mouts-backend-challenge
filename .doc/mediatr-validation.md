[Back to README](../README.md)

# MediatR and validation (application layer)

This document records **why** and **how** we validate application requests: a single path through the MediatR pipeline and FluentValidation, aligned with the registered `IPipelineBehavior`.

## Why this approach

Use-case validation is **centralized in the MediatR pipeline** instead of each handler constructing validators manually (`new XValidator()`).

Previously, `ValidationBehavior` was registered, but **`IValidator<TRequest>` implementations were not registered in DI**. The behavior received an empty validator list and did nothing. Handlers duplicated validation to compensate; some flows (e.g. authentication) never ran the Application-level validator before the handler.

We now have **one validation path**, consistent with documented MediatR + FluentValidation usage, and less risk that “what the pipeline should do” diverges from “what the handler actually does.”

**Light CQRS naming:** reads use `*Query` (e.g. `GetUserQuery`, `GetSaleByIdQuery`, `ListSalesQuery`); writes use `*Command`.

## Glossary

| Term | Meaning |
|------|--------|
| **MediatR** | In-process mediator: the controller calls `Send` with a request; MediatR resolves and runs the matching `IRequestHandler`. |
| **`IRequest<TResponse>`** | Request contract including response type (command or query in practice). |
| **`IRequestHandler<TRequest, TResponse>`** | Use-case implementation: handles the request and returns the result. |
| **`IPipelineBehavior<TRequest, TResponse>`** | Pipeline hook around every request (logging, validation, metrics, etc.). |
| **`ValidationBehavior`** | Our behavior that resolves `IEnumerable<IValidator<TRequest>>` from DI, runs FluentValidation, and throws `ValidationException` on failures. |
| **FluentValidation `AbstractValidator<T>`** | Declarative rules (`RuleFor`, …) for type `T` (usually the command/query). |
| **`AddValidatorsFromAssembly`** | FluentValidation DI extension that registers all `IValidator<>` implementations from an assembly (typically in host `Program.cs`). |
| **Command vs Query** | Command: mutating intent. Query: read-only. Optional in MediatR but improves readability and folder naming. |
| **`ApplicationLayer` (marker class)** | Empty type used only to obtain `typeof(ApplicationLayer).Assembly` for registering handlers and validators without magic strings. |

## What changed (summary)

1. **Host registration** — Package `FluentValidation.DependencyInjectionExtensions` (aligned with the FluentValidation version in use). In [`Program.cs`](../backend/src/Ambev.DeveloperEvaluation.WebApi/Program.cs): `services.AddValidatorsFromAssembly(typeof(ApplicationLayer).Assembly);` so Application validators are in DI.

2. **Handlers** — Removed manual validation blocks from affected Sales and Users handlers. Validation is performed by **`ValidationBehavior`** plus registered validators.

3. **`CreateUserCommand`** — Removed unused `Validate()` helper to avoid two competing validation entry points on the same request type.

4. **Naming** — `GetUserCommand` renamed to **`GetUserQuery`** (types, handler, validator, WebApi mappings).

5. **Tests** — Assertions that expected `ValidationException` from **calling the handler directly** were replaced or supplemented with **validator unit tests** (`CreateSaleValidator`, `ListSalesValidator`, `CreateUserCommandValidator`), which is the appropriate layer after this refactor.

## Why it matters

| Aspect | Rationale |
|--------|-----------|
| **Single responsibility** | Handlers focus on orchestration and domain rules; input validation lives in the pipeline + DI-registered validators. |
| **Configured behavior matches runtime** | `ValidationBehavior` is no longer ineffective relative to actual execution. |
| **Fewer gaps** | Flows that relied only on the handler (or skipped validation) are covered uniformly when an `AbstractValidator` exists for the request. |
| **Maintenance** | New features: add `AbstractValidator<TRequest>` in the Application assembly; DI and MediatR wire it automatically. |
| **Vocabulary** | `GetUserQuery` aligns read naming with Sales. |

## Request flow (reference)

```
Controller → IMediator.Send(request)
           → ValidationBehavior (FluentValidation via IValidator<> from DI)
           → IRequestHandler (no duplicate manual validation)
```

<br>
<div style="display: flex; justify-content: space-between;">
  <a href="./tech-stack.md">Previous: Tech Stack</a>
  <a href="./frameworks.md">Next: Frameworks</a>
</div>
