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

## Business Rules

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

How this repository implements the challenge (domain, APIs, validation, infrastructure) is summarized in [Technical decisions](/.doc/technical-decisions.md).

## Overview

This section provides a high-level overview of the project and the various skills and competencies it aims to assess for developer candidates.

See [Overview](/.doc/overview.md)

## Tech Stack

This section lists the key technologies used in the project, including the backend, testing, frontend, and database components.

See [Tech Stack](/.doc/tech-stack.md)

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

## Running the System

See [Running the System](/.doc/running-the-system.md) for the full runbook: Docker Compose (development and production-like), JWT and dev seed login, integration tests, and troubleshooting. Rationale for Mongo history, security, and bootstrap behavior is documented in [Technical decisions](/.doc/technical-decisions.md).

## Technical debt

- **Auth and Users vs Sales:** bring Auth and Users in line with the same patterns described in [Technical decisions](/.doc/technical-decisions.md) for Sales (MediatR pipeline validation on Application commands/queries, light CQRS naming, controller style). Application-layer handlers for Auth/Users already avoid manual `new XValidator()`; the remaining gap is mostly **WebApi controllers** that still validate request DTOs with `new …RequestValidator()` before `IMediator.Send`, whereas **Sales** maps the body and relies on the pipeline for the Application contract.
- **API tests:** functional tests already cover several **non-success** paths (for example invalid payloads returning **400**, missing resources returning **404**, and failed login returning **401**). Still worth expanding **authorization/JWT** coverage on **protected** routes: missing `Authorization`, malformed `Bearer` values, and invalid or expired tokens—systematically across **POST** and **GET**, not only the paths exercised with a valid seed token.

## Implementation narrative (read this for review)

**[Technical decisions](/.doc/technical-decisions.md)** is the authoritative document for how this repository interprets the challenge: Sales aggregate rules, MongoDB event history, JWT and Swagger, the MediatR validation pipeline, and layered tests. It also covers **simulated event publication**: sales domain events (`SaleCreated`, `SaleModified`, `SaleCancelled`, `ItemCancelled`, …) are published through `SimulatedSalesEventBroker` as structured logs after persistence—no external message broker, in line with the brief’s optional events differential. Reviewers should read it before diving into the codebase—it is the fastest path from the brief to the implementation choices and file-level pointers.
