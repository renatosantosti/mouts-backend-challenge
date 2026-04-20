[Back to README](../README.md)

## Technical decisions

This document records **implementation choices** for this codebase: what we decided beyond the evaluation brief, and where to look in code.

### Sales aggregate (domain)

The following rules are **implementation decisions** for the Sales aggregate (not all are spelled out in the challenge text). They are intentionally **simple** so the model matches what the evaluation brief implies, without extra workflows not required by the spec.

1. **External identities**  
   Customer, branch, and product are referenced only by **identifier + denormalized name** on the sale and line items. Other bounded contexts are not loaded inside the domain model.

2. **One active line per product on the sale**  
   There is at most **one non-cancelled line** per `ProductId` on a given sale. You do **not** create a second active line for the same product while an active line already exists.

   **Cancelled line:** If the only line for that `ProductId` is **cancelled** (logical cancellation), a later `AddItem` for the same product **creates a new active line**—the same as adding that product for the first time after the previous line was cancelled. Older cancelled rows remain in the aggregate only for history.

3. **Input validation (`AddItem`)**  
   The domain rejects invalid commands: **quantity must be greater than zero**, and **unit price must be greater than zero** (aligned with `SaleItem` rules).

4. **Adding the same product again when an active line already exists**  
   - **Sum** the new quantity into the existing line’s quantity.  
   - Set **unit price** and **product name** to the values from **this** `AddItem` call (last call wins).  
   - **No** cancelling and recreating the line when the price changes—**update the same line in place** and run discount/total logic again.  
   - If the **resulting** quantity would exceed **20**, the operation fails with a domain error (same “max 20 identical items” rule as in the brief).  
   Discount tiers apply to the **final** quantity and current unit price on that line.

5. **Discounts**  
   Computed **inside the domain** per line from `quantity × unit price`, using the tiers in the brief. Money rounding is fixed in code (e.g. two decimal places) for reproducibility.

6. **Domain events and simulated broker**  
   - A successful **`AddItem`** results in a **`SaleModified`** event (one per successful add).  
   - Other events (`SaleCreated`, `SaleCancelled`, `ItemCancelled`, etc.) follow the aggregate behavior described in code; handlers can log or forward them without requiring a message broker for the prototype.  
   - **Simulated broker:** event publication is intentionally simulated with structured application logs (`SimulatedSalesEventBroker.PublishAndClear`) after successful persistence in sales handlers (no external broker integration required by the challenge).

### Sales history (MongoDB)

- **PostgreSQL (EF Core)** remains the **primary store** for the `Sale` aggregate (create/update/query by id as implemented in the ORM layer).  
- **MongoDB** stores a **timeline of sale-related events** for history/read models. Writers and readers are implemented in [`MongoSaleEventHistoryRepository`](../backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/MongoSaleEventHistoryRepository.cs), registered in [`InfrastructureModuleInitializer`](../backend/src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs).  
- HTTP **sale history** responses are served from this MongoDB-backed history (see [`SalesController`](../backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs)).

### API security and Swagger

- Almost all API routes require a **JWT** (`Authorization: Bearer <token>`). The **anonymous** exception is **`POST /api/Auth`** (login).  
- Swagger UI uses a **Bearer** security scheme so you can authorize requests from the UI ([`SwaggerBearerSecurityOperationFilter`](../backend/src/Ambev.DeveloperEvaluation.WebApi/Swagger/SwaggerBearerSecurityOperationFilter.cs)); protected operations show a lock icon.

### Development bootstrap user

- In **Development** only, [`DevelopmentUserSeeder.EnsureSeedUserAsync`](../backend/src/Ambev.DeveloperEvaluation.WebApi/DevelopmentUserSeeder.cs) ensures a single seed user exists if no user with the seed e-mail is present.  
- E-mail and password constants live in [`DevelopmentAuthSeed`](../backend/src/Ambev.DeveloperEvaluation.Common/Security/DevelopmentAuthSeed.cs). These credentials are for **local/dev** use only, not production.

### Application layer — MediatR and FluentValidation

Use-case validation is **centralized in the MediatR pipeline** instead of each handler constructing validators manually (`new XValidator()`).

Previously, `ValidationBehavior` was registered, but **`IValidator<TRequest>` implementations were not registered in DI**. The behavior received an empty validator list and did nothing. Handlers duplicated validation to compensate; some flows (for example authentication) did not run Application-level validation before the handler.

We now have **one validation path**, consistent with documented MediatR + FluentValidation usage, and less risk that “what the pipeline should do” diverges from “what the handler actually does.”

Concrete changes:

- Package **`FluentValidation.DependencyInjectionExtensions`** (aligned with the FluentValidation version in use). In [`Program.cs`](../backend/src/Ambev.DeveloperEvaluation.WebApi/Program.cs): `services.AddValidatorsFromAssembly(typeof(ApplicationLayer).Assembly);` so Application validators are registered in DI.  
- **Handlers:** removed manual validation (`new XValidator()` + `ValidateAsync` + `throw ValidationException`) from affected Sales and Users handlers; **`ValidationBehavior`** plus DI-registered validators perform validation **before** the handler.  
- **`CreateUserCommand`:** removed the unused **`Validate()`** helper so the same request type does not expose two competing validation entry points for HTTP/MediatR.  
- **Naming (light CQRS):** **`GetUserCommand`** was renamed to **`GetUserQuery`** (types, handler, validator, WebApi mappings), consistent with `GetSaleByIdQuery` and `ListSalesQuery` in Sales.  
- **Tests:** where tests expected **`ValidationException`** by **calling the handler directly**, they were replaced or supplemented with **unit tests of validators** (`CreateSaleValidator`, `ListSalesValidator`, `CreateUserCommandValidator`), which is the appropriate layer after this refactor.

For the full rationale, glossary, request-flow diagram, and “why it matters” table, see [MediatR and validation (application layer)](./mediatr-validation.md).

### Layered testing strategy

We keep **fast, deterministic checks** close to the domain and push **I/O and multi-service behavior** to narrower suites, so regressions in business rules are caught without paying the cost of Docker on every test run.

- **Domain (DDD):** the aggregate and value rules are exercised **without** databases or HTTP—see [`SaleTests`](../backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs) and other tests under [`backend/tests/Ambev.DeveloperEvaluation.Unit/Domain`](../backend/tests/Ambev.DeveloperEvaluation.Unit/Domain) (validators, specifications). This is where discount tiers, quantity limits, and domain events stay honest.

- **Application and validation:** MediatR handlers are tested with **substituted** repositories and services ([`backend/tests/Ambev.DeveloperEvaluation.Unit/Application`](../backend/tests/Ambev.DeveloperEvaluation.Unit/Application)); request rules live in **FluentValidation** types tested directly (for example `CreateSaleValidator`, `ListSalesValidator`, `CreateUserCommandValidator`), aligned with the pipeline approach above and [MediatR and validation (application layer)](./mediatr-validation.md).

- **Functional (HTTP endpoints):** [`Microsoft.AspNetCore.Mvc.Testing`](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) hosts the API **in-process** while exercising real routes, serialization, auth wiring, and middleware. Factories and clients live under [`backend/tests/Ambev.DeveloperEvaluation.Functional/TestInfrastructure`](../backend/tests/Ambev.DeveloperEvaluation.Functional/TestInfrastructure); sales flows are in [`SalesEndpointsTests`](../backend/tests/Ambev.DeveloperEvaluation.Functional/Sales/SalesEndpointsTests.cs), with additional coverage under `Functional/Auth` and `Functional/Users`.

- **Integration (real stack):** tests run against the **Docker Compose** environment (PostgreSQL, MongoDB, WebApi) using an HTTP client fixture—see [`ComposeHttpFixture`](../backend/tests/Ambev.DeveloperEvaluation.Integration/Infrastructure/ComposeHttpFixture.cs) and [`SalesIntegrationTests`](../backend/tests/Ambev.DeveloperEvaluation.Integration/Sales/SalesIntegrationTests.cs). **How to start the stack and run this suite** is documented in [Running the System](./running-the-system.md) (subsection **3) Running Integration Tests**); unit and functional projects can be run anytime with `dotnet test` on [`Ambev.DeveloperEvaluation.Unit.csproj`](../backend/tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj) and [`Ambev.DeveloperEvaluation.Functional.csproj`](../backend/tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj) respectively.
