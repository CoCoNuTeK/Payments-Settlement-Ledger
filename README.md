# Payments Ledger — Event‑Driven Demo

A small event‑driven sample that simulates payments and streams live updates to a Blazor dashboard. It uses the Outbox pattern, Azure Service Bus (via Aspire), and a Blazor Server app that subscribes to integration events and updates the UI in real time.

## Tech Stack

- Blazor Web App (SSR + InteractiveServer)
- .NET 10 (net10.0) + ASP.NET Core
- Azure Service Bus (topics/subscriptions) via Azure.Messaging.ServiceBus (communication between microservices)
- .NET Aspire (AppHost) for local orchestration
- Entity Framework Core 10 + Npgsql (PostgreSQL)
- PostgreSQL (containers)
- Docker Desktop/Podman for local infra (Aspire depends on it)
- ASP.NET Core Identity (auth + roles)
- Background services (IHostedService/BackgroundService)
- Outbox pattern for reliable publishing
- Event‑driven pub/sub architecture
- In‑process channel bus for UI updates (communication inside of microservice)

## Observability (OpenTelemetry + Aspire)

- End‑to‑end tracing is wired using OpenTelemetry and shows up automatically in the Aspire dashboard.
- No local config required: Aspire sets OTEL_* env vars (service name/instance and `OTEL_EXPORTER_OTLP_ENDPOINT`).

What you’ll see in Traces (core flow)
- Payment Service
  - `presentation.payment.create` (producer, Presentation)
  - `inproc.message.process` (consumer, Infrastructure)
  - `application.payment.persist` (consumer, Application)
  - `infrastructure.payment.persist` (internal, Infrastructure) + EF Core DB spans (`paymentsdb`)
  - `servicebus.outbox.publish` (producer, Infrastructure) linked to the application span (across outbox boundary)
- Blazor
  - `servicebus.message.receive` (consumer, Infrastructure) linked to the publisher span
  - `inproc.message.process` (consumer, Infrastructure)
  - `presentation.payments.event.handle` (internal, Presentation)

How correlation works
- In‑proc: we pass `ActivityContext` through the internal envelope; consumer spans are true children.
- Outbox: we persist W3C `traceparent`/`tracestate` with each outbox row and create an ActivityLink on publish to relate traces across the async boundary.

Custom metrics
- Counter `payments.created` (Meter: `PaymentsLedger.PaymentService.Infrastructure`) increments after a successful payment persist.

Where it’s wired
- Payment Service OTel: `PaymentsLedger/PaymentService/PaymentsLedger.PaymentService.Infrastructure/Observability/ObservabilityRegistration.cs`
- Blazor OTel: `PaymentsLedger/BlazorWebApp/PaymentsLedger.Blazor.Infrastructure/Observability/ObservabilityRegistration.cs`

Prod note
- Point `OTEL_EXPORTER_OTLP_ENDPOINT` at an in‑cluster OpenTelemetry Collector (or a managed backend). Sampling is 100% in dev; consider reducing in prod.

## Overview

- Solution layout
  - Payment Service
    - Domain, Application, Infrastructure, Presentation projects
    - Outbox table, publisher to Service Bus topic `payments`
  - Blazor Web App
    - Presentation, Infrastructure, Application, Domain projects
    - Subscribes to `payments` topic (subscription `blazor-sub`), updates dashboard
  - Shared Kernel
    - Messaging primitives and shared integration contracts (DTOs)
  - AppHost (Aspire)
    - Orchestrates everything locally (projects, databases, Service Bus emulator)

- Core flow (happy path)
  - Simulator creates payments
  - Service saves payment + outbox, publishes to `payments`
  - Blazor subscribes and updates per‑merchant stats
  - Dashboard redraws a small live chart

- Contracts and routing (quickly)
  - We send a compact PaymentCreatedEvent JSON to topic `payments`; Blazor routes by Subject and updates the UI.

## Prerequisites

- .NET SDK 10 (preview/RC) — the solution targets `net10.0`.
- Docker Desktop (or compatible) — used by Aspire to run local infra (PostgreSQL and Service Bus emulator).
  

## Run It

- Clone the repo:

  ```bash
  git clone https://github.com/CoCoNuTeK/Payments-Settlement-Ledger.git
  cd Payments-Settlement-Ledger
  ```

- Start the Aspire AppHost:

  ```bash
  dotnet run -p PaymentsLedger/Aspire/PaymentsLedger.Aspire/PaymentsLedger.Aspire.csproj
  ```

- Open the Blazor app and sign in:
  - Navigate to `https://localhost:7279/login` (dev HTTPS port), or use the Aspire dashboard link to the Blazor endpoint.
  - Demo users are seeded automatically:
    - standard@demo.local / Passw0rd!
    - premium@demo.local / Passw0rd!

- Open Dashboard
  - Go to `/dashboard`. You’ll see:
    - Role badge (Standard or Premium)
    - Counters per merchant
    - A simple, scrollable bar chart of the latest events (per current user’s merchant)

- Behind the scenes
  - Two local PostgreSQL databases (Blazor Identity + Payments) run in containers.
  - Azure Service Bus runs as a local emulator via Aspire.
  - A background simulator in the Payment Service keeps generating payments.
  - Heads‑up: the simulator publishes ~10 payments every 10 seconds (~1/sec). Don’t leave it running for long test sessions or your local DB and traces will grow quickly.

## Notable Files

- Publisher side
  - Payment + outbox write (single SaveChanges): `PaymentsLedger/PaymentService/PaymentsLedger.PaymentService.Application/Aggregates/PaymentAggregate/Commands/PaymentCreate/PaymentCreatedCommandHandler.cs`
  - Outbox publisher to Service Bus: `PaymentsLedger/PaymentService/PaymentsLedger.PaymentService.Infrastructure/Messaging/ServiceBus/OutboxPublisherHostedService.cs`
  - Topic routing: `PaymentsLedger/PaymentService/PaymentsLedger.PaymentService.Infrastructure/Messaging/ServiceBus/IntegrationEventRouting.cs`

- Subscriber side
  - Service Bus processor: `PaymentsLedger/BlazorWebApp/PaymentsLedger.Blazor.Infrastructure/Messaging/ServiceBus/PaymentsEventsSubscriberHostedService.cs`
  - Event routing map: `PaymentsLedger/BlazorWebApp/PaymentsLedger.Blazor.Infrastructure/Messaging/ServiceBus/IncomingIntegrationEventRouting.cs`
  - UI event handler (per‑merchant stats): `PaymentsLedger/BlazorWebApp/PaymentsLedger.Blazor.Presentation/UI/Events/PaymentsEventHandler.cs`
  - Dashboard: `PaymentsLedger/BlazorWebApp/PaymentsLedger.Blazor.Presentation/Components/Pages/Dashboard.razor`

- Shared
  - DTO contract: `PaymentsLedger/SharedKernel/PaymentsLedger.SharedKernel/Contracts/IntegrationEvents/PaymentCreatedEvent.cs`
  - In‑proc envelope and bus: `PaymentsLedger/SharedKernel/PaymentsLedger.SharedKernel/Messaging/*`

## Design Notes

- Outbox pattern
  - The Payment aggregate and the OutboxIntegrationEvent row are persisted together. The Service Bus send happens out‑of‑transaction and is retried until successful.

- Per‑merchant view in UI
  - Blazor keeps a per‑merchant in‑memory snapshot to update the dashboard quickly. For production, you’d move this to a durable read model (API + cache) and push deltas via SignalR.

- Routing strategy
  - Publisher uses a simple event→topic dictionary.
  - Subscriber uses a simple event→handler mapping (and deserializes to the shared DTO).

- Logging
  - Both services are configured to log Warning and above by default to reduce noise.

## Troubleshooting

- Ports
  - Make sure the dev HTTPS port (7279) is available when running the presentation project, and the README points to `/login` on HTTPS for convenience.

- Docker
  - Ensure Docker Desktop/Podman is running before starting AppHost. Aspire will create local containers for PostgreSQL and the emulator.


## License

For demo/educational use.
