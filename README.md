# Developer Evaluation Project

`READ CAREFULLY`

## Use Case
**You are a developer on the DeveloperStore team. Now we need to implement the API prototypes.**

As we work with `DDD`, to reference entities from other domains, we use the `External Identities` pattern with denormalization of entity descriptions.

Therefore, you will write an API (complete CRUD) that handles sales records. The API needs to be able to inform:

* Sale number
* Date when the sale was made
* Customer
* Total sale amount
* Branch where the sale was made
* Products
* Quantities
* Unit prices
* Discounts
* Total amount for each item
* Cancelled/Not Cancelled

It's not mandatory, but it would be a differential to build code for publishing events of:
* SaleCreated
* SaleModified
* SaleCancelled
* ItemCancelled

If you write the code, **it's not required** to actually publish to any Message Broker. You can log a message in the application log or however you find most convenient.

### Business Rules

* Purchases above 4 identical items have a 10% discount
* Purchases between 10 and 20 identical items have a 20% discount
* It's not possible to sell above 20 identical items
* Purchases below 4 items cannot have a discount

These business rules define quantity-based discounting tiers and limitations:

1. Discount Tiers:
   - 4+ items: 10% discount
   - 10-20 items: 20% discount

2. Restrictions:
   - Maximum limit: 20 items per product
   - No discounts allowed for quantities below 4 items

### Sales domain: technical and product assumptions

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

6. **Domain events**  
   - A successful **`AddItem`** results in a **`SaleModified`** event (one per successful add).  
   Other events (`SaleCreated`, `SaleCancelled`, `ItemCancelled`, etc.) follow the aggregate behavior described in code; handlers can log or forward them without requiring a message broker for the prototype.
   - In this implementation, event publication is intentionally simulated with structured application logs (`SimulatedSalesEventBroker.PublishAndClear`) after successful persistence in sales handlers (no broker integration required by the challenge).

## Overview
This section provides a high-level overview of the project and the various skills and competencies it aims to assess for developer candidates. 

See [Overview](/.doc/overview.md)

## Tech Stack
This section lists the key technologies used in the project, including the backend, testing, frontend, and database components. 

See [Tech Stack](/.doc/tech-stack.md)

## Technical decisions (application layer)

**MediatR validation pipeline.** Application requests are validated by FluentValidation **before** handlers run: validators are registered via `AddValidatorsFromAssembly` on the Application assembly, and `ValidationBehavior` runs them through the MediatR pipeline. Handlers do not instantiate validators manually. This keeps a single validation path and matches the registered `IPipelineBehavior`.

**CQRS naming.** Read operations use `*Query` types (e.g. `GetUserQuery`, `GetSaleByIdQuery`) and writes use `*Command`, consistent across features.

For the full rationale, glossary, and verification notes, see [MediatR and validation (application layer)](/.doc/mediatr-validation.md).

## Frameworks
This section outlines the frameworks and libraries that are leveraged in the project to enhance development productivity and maintainability. 

See [Frameworks](/.doc/frameworks.md)

<!-- 
## API Structure
This section includes links to the detailed documentation for the different API resources:
- [API General](./docs/general-api.md)
- [Products API](/.doc/products-api.md)
- [Carts API](/.doc/carts-api.md)
- [Users API](/.doc/users-api.md)
- [Auth API](/.doc/auth-api.md)
-->

## Project Structure
This section describes the overall structure and organization of the project files and directories. 

See [Project Structure](/.doc/project-structure.md)

## Running with Docker Compose (Development)

Use this flow to run the complete backend stack (WebApi + PostgreSQL + MongoDB + Redis) with environment variables.

1. Create your development env file from the example:
   - PowerShell:
     - `Copy-Item backend/.env.example backend/.env.development`
2. Update secrets and local values in `backend/.env.development`.
   - If your Mongo password contains special characters, keep `MONGO_PASSWORD_ENCODED` URL-encoded.
3. Start the stack with the development compose layer:
   - `docker compose --env-file backend/.env.development -f backend/docker-compose.yml -f backend/docker-compose.override.yml up --build`
4. Stop and remove containers:
   - `docker compose --env-file backend/.env.development -f backend/docker-compose.yml -f backend/docker-compose.override.yml down`

Notes:
- `docker-compose.yml` is parameterized and does not keep hardcoded passwords.
- `docker-compose.override.yml` is the development layer and points services to `backend/.env.development`.
- ASP.NET Core reads variables like `ConnectionStrings__DefaultConnection` and `Mongo__ConnectionString`, overriding values from `appsettings.json`.
- `backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json` keeps only non-sensitive placeholders for connection strings and JWT secret.
