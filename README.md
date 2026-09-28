# Test assignment: partner commission system

**English** | [Русский](README.ru.md)

Users form a partner tree. When a user makes a profit, the partners above them (up to 10 levels) earn commissions under
the Linear or Fibonacci schema, and accrued commissions are paid out to their wallets on a schedule. I built it as five
microservices behind an API Gateway.

**Stack:** .NET 10 (ASP.NET Core Web API with controllers, gRPC, EF Core), PostgreSQL 17, Apache Kafka 4.1 (KRaft), YARP,
FluentValidation, Scalar, Polly, OpenTelemetry, Aspire Dashboard, Docker Compose, xUnit v3.

## Contents

1. [Getting started](#1-getting-started)
2. [Architecture](#2-architecture)
3. [Payout saga](#3-payout-saga)
4. [Reliability](#4-reliability)
5. [Scaling and evolution](#5-scaling-and-evolution)
6. [Reference](#6-reference)

---

## 1. Getting started

### 1.1. Requirements

All you need is Docker with the `compose` v2 and `buildx` plugins and about 4 GB of free memory. Everything is built
inside Docker, so there is no need to install the .NET SDK.

### 1.2. Running the system

```bash
docker compose up -d --build
docker compose ps
```

The first start takes 1-2 minutes. Once all .NET services are `healthy`, the system is ready. Database schemas are
created by EF Core migrations when the services start, and Kafka topics are created by the one-off `kafka-init`
container.

| Component | Address |
|---|---|
| API Gateway and API documentation (Scalar) | http://localhost:8080 |
| Aspire Dashboard (traces, metrics, logs) | http://localhost:18888 |
| Kafka UI | http://localhost:8088 |
| pgAdmin (`admin@valetaxtest.dev` / `admin`, server preconfigured) | http://localhost:5050 |
| PostgreSQL (`postgres` / `postgres`) | `localhost:5432` |
| Kafka for external clients | `localhost:29092` |

Ports can be changed with the environment variables `GATEWAY_PORT`, `DASHBOARD_PORT`, `KAFKA_UI_PORT`, `PGADMIN_PORT`,
`POSTGRES_PORT` and `KAFKA_EXTERNAL_PORT`, and the payout period with `PAYOUT_INTERVAL` (default `00:00:30`).

| Action | Command |
|---|---|
| Stop | `docker compose down` |
| Stop and remove data | `docker compose down -v` |
| Run several instances of a service | `docker compose up -d --scale commissions=3 --scale partners=2` |
| Unit tests (requires the .NET 10 SDK) | `dotnet test` |

### 1.3. Demo scenario

For a quick check I wrote `scripts/demo.sh` (requires bash and curl). It runs an end-to-end scenario through the
gateway and prints the expected values next to the API responses.

The script builds the chain `root ← a ← b ← c ← d` and registers a profit of 1000 for `d`. Under the Linear schema c, b,
a and root receive 10, 20, 30 and 40, and under the Fibonacci schema 10, 10, 20 and 30. After the payout their balances
are 20, 30, 50 and 70. At the end the script blocks the wallet of `a` and shows how the payout saga rolls the payout
back.

---

## 2. Architecture

### 2.1. Services

I split the system into five services. Each one has its own database and accesses data of other services only through
their APIs or messages. In docker-compose all databases live on one PostgreSQL server under separate accounts.

| Service | Purpose | Data |
|---|---|---|
| **Partners** | Users, partner links, cycle prevention, tree branches | `users` |
| **Events** | Profit event intake, event list and details | `profit_events`, copies of commissions and payments |
| **Commissions** | Commission calculation, accrual ledger, accrual schema, reserving commissions for payouts | `commissions`, `processed_events`, `commission_settings`, `schema_changes`, `payout_reservations` |
| **Wallets** | Wallets, crediting payouts, payout history, wallet blocking | `wallets`, `wallet_payouts`, `wallet_payout_items` |
| **Payouts** | Scheduled payouts, saga orchestration, step timeouts | `payout_sagas` |
| **Gateway** | Single entry point on YARP, shared Scalar page | - |

I separated event intake from commission calculation. Events stores an event and replies `202 Accepted` right away,
while the calculation, which needs the partner chain, runs asynchronously and scales on its own. If Partners or
Commissions is down, events wait in Kafka.

Accruals and balances also live in different services. A payout changes data in both Commissions and Wallets, so I made
it a saga coordinated by a separate Payouts service. The saga participants know nothing about each other.

### 2.2. Service internals

**Partners** stores only the direct partner of each user and reads tree branches with recursive CTEs.

**Events** stores an event and the message for Commissions in one transaction ([section 4.2](#42-transactional-outbox)).
It checks the event owner's registration in Partners and caches positive answers in the replica's memory for 5 minutes.
Event details come from the Events read model, which holds copies of commissions and payments received from Commissions,
so reading them does not depend on Commissions availability.

**Commissions** calculates commissions using schemas that I implemented with the Strategy pattern.
`LinearCommissionSchema` and `FibonacciCommissionSchema` implement `ICommissionSchema`, and `CommissionSchemaResolver`
picks the right one by the type stored in `commission_settings`. `CommissionCalculator` depends only on the interface,
so a new schema is just a new class with no changes to the calculation. In the payout saga Commissions reserves
commissions, confirms their payment or returns them to the accrued state.

**Wallets** stores the balance in a column and changes it in the same transaction as the payout record. The crediting
result is stored by `payout_id`, so a repeated command gets the same reply.

**Payouts** runs the payout saga. States and transitions are defined by the `PayoutSagaStateMachine` finite state
machine, and scheduled runs and resending of unanswered commands are handled by background jobs
([section 3](#3-payout-saga)).

### 2.3. Service communication

| Channel | Purpose | Properties |
|---|---|---|
| REST/JSON through YARP | External API | OpenAPI, single documentation page |
| gRPC | Synchronous reads of another service's data | `.proto` contract, deadlines, balancing across replicas |
| Kafka | Propagation of changes between services (events and saga commands) | Independence from receiver availability, ordering by key, redelivery |

I use gRPC only for reads of data that must be current at the time of the call.

Main Kafka flows:
1. **User registration.** Partners publishes `partners.user-registered`, and Wallets opens a wallet.
2. **Profit event.** Events publishes `events.profit-event-registered`, Commissions calculates commissions and publishes
   `commissions.commissions-calculated`, and Events stores the result in its read model.
3. **Payout.** Payouts exchanges commands and replies with Commissions and Wallets ([section 3](#3-payout-saga)). After
   the payout Commissions publishes `commissions.commissions-paid`, and Events marks the commissions as paid.

I used choreography for user registration and commission calculation since they need no compensation, and
orchestration for the payout.

### 2.4. Service structure

Each service consists of four projects following DDD and Clean Architecture. Dependencies point inward (`API` →
`Infrastructure` → `Application` → `Domain` → `Core`), and the compiler enforces this through project references.

| Project | Contents |
|---|---|
| `API` | `Program.cs` with dependency registration, controllers, gRPC server and `.proto`, DTO to gRPC mapping |
| `Infrastructure` | EF Core, migrations, repositories, read queries, gRPC adapters, background jobs |
| `Application` | Application services, validators, message handlers, message contracts, ports, mappers |
| `Domain` | Entities and domain services |
| `Core` (shared library) | `Money`, `ExternalIds`, `DomainException`, Application exceptions, storage ports |

**Domain model.** I use an anemic model. Entities hold only data, and business rules live in domain services without
infrastructure dependencies.

**Application services.** Each area has one application service (`IUserService`, `IEventService`,
`ICommissionService`, `IWalletService`, `IPayoutService`), with one method per API operation. Kafka messages are handled
by `IMessageHandler<T>` classes, which play the same role for messages as controllers do for HTTP.

**Validation.** Requests are defined as `<Operation>Request` and `<Operation>RequestValidator` pairs using
FluentValidation. The application service calls `ValidateAndThrowAsync`, so the same rules apply to REST and gRPC. A
validation error is returned as `400`, or as `InvalidArgument` in gRPC.

**Ports and adapters.** The Application layer knows nothing about EF Core, gRPC or Kafka. It declares ports, and their
implementations live in Infrastructure. Writes go through repositories and `IUnitOfWork` in one transaction together
with `IOutbox`. Reads go through separate `I*Reader` interfaces, which load entities without change tracking and turn
them into DTOs. Other services are hidden behind the `IPartnerHierarchy`, `IUserDirectory` and `ICommissionsLedger`
ports, implemented by gRPC adapters.

**Mapping.** I implemented conversions between models as extension methods in the `Mappings` folder of the layer that
can access both models. The file name reflects the direction of the conversion.

**Message contracts.** Each service declares the messages it publishes or receives (`Application/LinkEvents`, saga
messages, `Topics.cs`). Messages are serialized as JSON, and the type name goes in the `message-type` header. The
`.proto` file lives in the server project, and clients reference it as a linked file.

**Shared libraries.** I moved only infrastructure code to `src/Libraries`.

| Library | Contents |
|---|---|
| `EventBus` | Messaging abstractions |
| `MessageLogEF` | Outbox and inbox tables |
| `EventBusKafka` | Kafka producer, relay and consumers |
| `PersistencePostgres` | DbContext, Unit of Work, advisory locks, migrations |
| `ValetaxTest.ServiceDefaults` | Logging, OpenTelemetry, health checks, error handling, gRPC clients with Polly |

### 2.5. Data storage

| Database | Tables and constraints |
|---|---|
| `partners` | `users(external_id PK, partner_id FK → users)`, index on `partner_id` |
| `events` | `profit_events(external_id PK)`, `event_commissions`, `commission_payments` |
| `commissions` | `commissions` with a unique index on `(event_id, level)` and a partial index on `(partner_id, accrued_at) WHERE status = 'Accrued'`, `processed_events(event_id PK)`, `commission_settings` (single row), `schema_changes`, `payout_reservations(payout_id PK)` |
| `wallets` | `wallets(user_id PK, CHECK balance ≥ 0)`, `wallet_payouts(payout_id PK)`, `wallet_payout_items(commission_id PK)` |
| `payouts` | `payout_sagas` with a partial unique index allowing one active payout per partner and an index on `next_attempt_at` of active sagas |

Every service has an `outbox_messages` table, and consumers also have `inbox_messages`. I store money as
`numeric(24,8)`.

---

## 3. Payout saga

The payout is the only distributed transaction in the system. Commissions have to be marked as paid in Commissions and
the amount credited to the wallet in Wallets without paying anything twice, and if the wallet rejects the credit, the
commissions have to return to the accrued state.

### 3.1. Choice of orchestration

I chose orchestration because the saga steps are not equivalent. The reservation can be rolled back, crediting is the
point of no return, and the confirmation must complete no matter what. This logic is easier to express as one finite
state machine, `PayoutSagaStateMachine`, than as a chain of subscriptions across three services.

The state of each payout is stored explicitly in a `payout_sagas` row together with the current step, the retry count
and the transition history (`GET /api/payouts/{id}`). Timeouts and retries are handled in one place, the participants
are unaware of each other, and a new step is added only to Payouts.

### 3.2. Timeouts

If no reply arrives within `StepTimeout` (30 seconds), the timeout monitor resends the command of the current step. The
delay doubles every time, up to 5 minutes.

I do not compensate the saga on a timeout. The result of a credit command is unknown until the reply arrives, and a
rollback followed by a new payout could pay twice. Compensation runs only when the wallet explicitly rejects the credit.

### 3.3. Processing order

Commands and replies are sent with the `payoutId` key, and commands to the wallet with the `userId` key, so messages of
one payout are processed in order. The saga row stays locked while a reply is applied. A transition runs only from the
state that expects the reply, so duplicate and late replies are ignored.

---

## 4. Reliability

### 4.1. Idempotency

Kafka delivers messages at least once, and a client may repeat a request, so I added duplicate protection at every
level.

| Level | Mechanism |
|---|---|
| Event intake | Primary key `profit_events.external_id`. A repeated request gets the stored event, and concurrent identical requests are resolved by the same key |
| Message delivery | Inbox. A `(message_id, consumer)` row is inserted with `ON CONFLICT DO NOTHING` in the same transaction as the handler's changes, and a repeated message is skipped |
| Accrual | `processed_events(event_id PK)` ensures that an event is accrued once, even if it arrives in another message. The unique index `commissions(event_id, level)` adds another safeguard |
| Reservation | One active saga per partner. `SKIP LOCKED` keeps two reservations from taking the same commission. A repeated command gets the reservation snapshot by `payout_id` |
| Crediting | The result is stored by `payout_id`. `wallet_payout_items(commission_id PK)` prevents a commission from being credited twice |
| Saga transitions | A reply is applied only in the expected state |

### 4.2. Transactional outbox

Services do not write to Kafka directly. A message is saved to `outbox_messages` in the same transaction as the data,
and the `OutboxRelay` background job in each replica sends it on:
1. takes up to 100 rows with `FOR UPDATE SKIP LOCKED`, so replicas do not get in each other's way;
2. sends the batch with an idempotent producer using `acks=all`;
3. deletes the sent rows.

After an error the relay pauses and doubles the pause up to 30 seconds. If the process crashes between sending and
deleting, the message goes out again and the receiver's inbox drops it. The trace context is saved with the message, so
asynchronous processing shows up as one trace.

### 4.3. Message processing

Messages are processed by `KafkaConsumerService`. The handler runs in the same transaction as the inbox insert, and the
offset is committed only after successful processing or after the message is moved to the DLT.

| Situation | Behavior |
|---|---|
| Transient error (database, broker or another service unavailable, circuit breaker open) | Three in-process attempts, then `Seek` to the same message and a retry with a delay from 1 to 30 seconds. The message is not lost, and the partition does not advance |
| Permanent error (unknown type, invalid JSON, failure after three attempts) | The message is moved to the dead letter topic `<topic>.dlt`. The headers keep the reason, the error text, and the original topic, partition and offset |

Inbox rows are kept for 8 days, which is longer than Kafka keeps messages (7 days by default). A background job deletes
old rows every hour.

A payment fact can reach Events before the accrual fact, so I store payments separately in `commission_payments` and
join them with commissions on read.

### 4.4. Synchronous calls

Every gRPC call goes through a Polly pipeline:
1. Total timeout of 10 s.
2. Retry with 3 attempts, exponential backoff and jitter, for transient errors only.
3. Circuit breaker that opens for 15 s when 50% of at least 5 calls within 30 s fail.
4. Per-attempt timeout of 3 s.

If Partners is down, Events accepts events of users found in the cache and replies `503` with the `Retry-After` header
to the rest. Commission calculation pauses meanwhile and resumes on its own once Partners is back.

### 4.5. Graceful shutdown

On SIGTERM a service:
- switches the readiness check to `Unhealthy`;
- lets Kestrel finish the requests in progress;
- lets Kafka consumers finish the current message, commit the offset and leave the group, so their partitions move to
  other replicas right away;
- waits for the relay to send the current batch and calls `Flush` on the producer;
- rolls back unfinished transactions of the Payouts background jobs.

The shutdown time is set by `Service:ShutdownTimeout` (30 s), and in docker-compose `stop_grace_period` is 40 s.

### 4.6. Logs, metrics, traces

Traces, metrics and logs go over OTLP to the Aspire Dashboard. One trace covers the whole path through the gateway, the
service, SQL, Kafka, the consumer and gRPC. Logs are also written to stdout as JSON with `TraceId`, and during message
processing also with `MessageId` and `CorrelationId` (the event or payout id).

I added custom metrics with the `valetaxtest.*` prefix for event intake and processing, accruals by schema, payout sagas
and their step retries, wallet credits, message publishing and processing, and DLT. Metrics of ASP.NET Core,
HttpClient, the runtime, Npgsql and Polly are collected as well.

| Endpoint | Checks | Purpose |
|---|---|---|
| `/health/live` | The process | Liveness without external dependencies, so a shared database failure does not restart the containers |
| `/health/ready` | Database, Kafka, service not shutting down | Healthcheck in docker-compose |

---

## 5. Scaling and evolution

The services keep no state in memory (apart from a small cache in Events), so any of them can run in several instances
with `docker compose up -d --scale <service>=N`. The main limitation is that Kafka, PostgreSQL and the Aspire Dashboard
run as single instances. I would also split each service and the API Gateway into separate repositories.

---

## 6. Reference

### 6.1. REST API

The full API description is available in Scalar on the gateway (`http://localhost:8080/scalar`), one document per
service. Errors are returned in the RFC 7807 format.

| Code | Meaning |
|---|---|
| `400` | Validation error, the fields are listed in `errors` |
| `404` | Resource not found |
| `409` | Conflict |
| `422` | Business rule violation |
| `503` | A dependency is unavailable, the response includes the `Retry-After` header |

Lists return a JSON array and accept `page` (from 1) and `pageSize` (1 to 100, default 20). A value out of range returns
`400`.

### 6.2. Configuration

Settings live in the `appsettings.json` of each service, and in docker-compose I override them with environment
variables (for example, `Payouts__Interval`). By default database, Kafka, gRPC and OTLP addresses point to `localhost`,
so a service can be started with `dotnet run`. Settings are validated at startup.

| Key | Default | Service |
|---|---|---|
| `Commissions:MaxLevels` | `10` | commissions |
| `PartnerTree:MaxDepth`, `PartnerTree:MaxDownlineSize` | `10`, `1000` | partners |
| `Payouts:Interval` | `00:01:00` (`00:00:30` in compose) | payouts |
| `Payouts:StepTimeout`, `Payouts:MaxRetryDelay`, `Payouts:TimeoutCheckInterval` | `30s`, `5m`, `10s` | payouts |
| `Payouts:MaxItemsPerPayout`, `Payouts:MaxPartnersPerRun`, `Payouts:SchedulerEnabled` | `500`, `1000`, `true` | payouts |
| `Outbox:PollingInterval`, `Outbox:BatchSize`, `Outbox:BatchTimeout` | `200ms`, `100`, `15s` | all except the gateway |
| `Service:ShutdownTimeout` | `00:00:30` | all |
