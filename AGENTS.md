# Payments & Settlement Ledger - Root Instructions file

## Domain Context

This project simulates how an online payment processor manages transactions, settlements, and merchant payouts in a secure and auditable way. It represents the core backend of a fintech platform connecting merchants, customers, and banks.

---

## Architecture Overview

> ⚡ The frontend (WASM) receives live updates from backend services via **SignalR**, enabling **eventual consistency** across the system without tight coupling.

### Frontend (Blazor WASM)

* During **development**, the Blazor WASM frontend is orchestrated together with backend services through **Aspire** for unified local development, telemetry, and service discovery.
* In **production**, the Blazor WASM app is **deployed separately to a CDN**, consuming backend APIs over HTTPS.
* TODO: Define FE-specific architectural conventions, component organization, and API integration patterns.

### Backend (Microservices)

We follow a microservices-based system using **Clean Architecture principles**, orchestrated by **Aspire** in development environments.

When writing or modifying backend code, you **must first read and follow** the rules in the relevant layer instruction file before committing any changes in that particular layer:

* **llm_instructions/presentation_layer.md**
* **llm_instructions/application_layer.md**
* **llm_instructions/domain_layer.md**
* **llm_instructions/infrastructure_layer.md**

When writing any kind of test (unit, integration), you **must first read and follow** the guidelines defined in:

* **llm_instructions/backend_testing_rules.md** This ensures consistent testing strategy across microservices.

We also implement our own **Result** pattern (e.g., `Result<object>`) for consistent success/error propagation across layers and services.

**Domain Events:** For implementing domain event flow (within a single microservice, between aggregates), always read and follow **llm_instructions/domain_events.md**.

**Integration Events:** For publishing external messages to communicate with other micro services these are indirectly invoked by domain events where there is a handler of the domain event that instantiates the integration event and writes it in the outbox table. Then there is a publisher service running in the background that checks the outbox table and publishes the events to bus, queue, topic or stream.

---

###
