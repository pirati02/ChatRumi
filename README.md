# ChatRumi

A microservices-based real-time chat and social platform built with .NET 11, featuring event sourcing, CQRS, and distributed tracing.

---

## Overview

ChatRumi is a production-grade backend platform providing real-time messaging, friend connections, and a social feed. It leverages Domain-Driven Design, event-driven architecture, and modern cloud-native patterns across four independent microservices behind a unified API gateway.

## Features

- **Real-time messaging** — 1-on-1 and group chats with delivery/read receipts via SignalR
- **Friend connections** — Send, accept, and reject friend requests stored as a graph in Neo4j
- **Social feed** — Create and browse posts, indexed and searchable with Elasticsearch
- **Account management** — Registration, verification, profile updates, and public key storage for E2E encryption
- **Event sourcing** — Full audit trail of domain events persisted with Marten on PostgreSQL
- **Service discovery** — Automatic registration and health checks through Consul
- **Distributed tracing** — End-to-end observability with OpenTelemetry and Jaeger
- **Event-driven sync** — Cross-service data consistency via Kafka consumers and RabbitMQ messaging

## Architecture

```
                         ┌──────────────┐
                         │   Clients    │
                         └──────┬───────┘
                                │
                         ┌──────▼───────┐
                         │   Gateway    │  :7000
                         │   (Ocelot)   │
                         └──────┬───────┘
                                │
              ┌─────────────────┼─────────────────┐
              │                 │                  │
     ┌────────▼──────┐  ┌──────▼───────┐  ┌──────▼───────┐  ┌──────────────┐
     │   Account     │  │    Chat      │  │  Friendship  │  │     Feed     │
     │   Service     │  │   Service    │  │   Service    │  │   Service    │
     │   :5049       │  │   :5111      │  │   :5031      │  │   :5211      │
     └───────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘
             │                 │                  │                 │
     ┌───────▼───────┐  ┌─────▼──────┐   ┌──────▼───────┐  ┌─────▼──────┐
     │  PostgreSQL   │  │ PostgreSQL │   │    Neo4j     │  │Elasticsearch│
     │  (Marten)     │  │  + Redis   │   │   (Graph)    │  │  (Search)   │
     └───────────────┘  └────────────┘   └──────────────┘  └─────────────┘
             │                 │                  │                 │
             └─────────────────┴──────────────────┴─────────────────┘
                                       │
                    ┌──────────────┬────┴────┬──────────────┐
                    │   Kafka      │ RabbitMQ│   Consul     │
                    │  (Events)    │  (Bus)  │ (Discovery)  │
                    └──────────────┴─────────┴──────────────┘
```

Each service follows a clean layered structure:

| Layer              | Responsibility                                       |
|--------------------|------------------------------------------------------|
| **Api**            | REST endpoints, SignalR hubs, middleware              |
| **Application**    | Use cases, CQRS command/query handlers, DTOs         |
| **Domain**         | Aggregates, entities, value objects, domain events    |
| **Infrastructure** | Persistence, messaging, external service integration |
| **AccountSync**    | Background Kafka consumer for cross-service sync     |

## Tech Stack

| Category           | Technology                                          |
|--------------------|-----------------------------------------------------|
| Language/Framework | .NET 11, ASP.NET Core Minimal APIs, C#              |
| Real-time          | SignalR                                             |
| CQRS / Mediator    | MediatR                                             |
| Event Sourcing     | Marten (PostgreSQL)                                 |
| Message Bus        | MassTransit + RabbitMQ                              |
| Event Streaming    | Apache Kafka                                        |
| Graph Database     | Neo4j                                               |
| Search Engine      | Elasticsearch                                       |
| Caching            | Redis                                               |
| API Gateway        | Ocelot                                              |
| Service Discovery  | Consul                                              |
| Tracing            | OpenTelemetry + Jaeger                              |
| Validation         | FluentValidation                                    |
| Containerization   | Docker, Docker Compose                              |

## Project Structure

```
ChatRumi/
├── src/
│   ├── Account/                         # Account management microservice
│   │   ├── ChatRumi.Account.Api/
│   │   ├── ChatRumi.Account.Application/
│   │   ├── ChatRumi.Account.Domain/
│   │   └── ChatRumi.Account.Infrastructure/
│   ├── Chat/                            # Real-time chat microservice
│   │   ├── ChatRumi.Chat.Api/
│   │   ├── ChatRumi.Chat.Application/
│   │   ├── ChatRumi.Chat.Domain/
│   │   ├── ChatRumi.Chat.Infrastructure/
│   │   └── ChatRumi.Chat.AccountSync/
│   ├── Friendship/                      # Friendship graph microservice
│   │   ├── ChatRumi.Friendship.Api/
│   │   ├── ChatRumi.Friendship.Application/
│   │   ├── ChatRumi.Friendship.Domain/
│   │   └── ChatRumi.Friendship.AccountSync/
│   ├── Feed/                            # Social feed microservice
│   │   ├── ChatRumi.Feed.Api/
│   │   ├── ChatRumi.Feed.Application/
│   │   ├── ChatRumi.Feed.Domain/
│   │   ├── ChatRumi.Feed.Infrastructure/
│   │   └── ChatRumi.Feed.AccountSync/
│   ├── ChatRum.Gateway/                 # Ocelot API gateway
│   ├── ChatRum.InterCommunication/      # Shared: Consul, OpenTelemetry, Kafka
│   ├── ChatRumi.Infrastructure/         # Shared infrastructure utilities
│   └── ChatRumi.Kernel/                 # Shared domain primitives
├── tests/                               # Unit & integration tests
├── docker-compose.yml
├── .env.example
└── Directory.Packages.props             # Centralized NuGet versions
```

## Getting Started

### Prerequisites

- [Docker & Docker Compose](https://docs.docker.com/get-docker/)
- [.NET 11 SDK](https://dotnet.microsoft.com/download) (for local development)

### Quick Start — Full Stack

Spin up everything (infrastructure + all services) with a single command:

```bash
docker compose up -d
```

### External Infrastructure Mode

If you already have databases and message brokers running elsewhere:

```bash
cp .env.example .env
# Edit .env with your connection details
docker compose up -d account-service chat-service friendship-service feed-service gateway
```

> **Tip:** Use `host.docker.internal` on Windows/macOS to reach services on the host machine.

### Local Development

Run any service individually:

```bash
cd src/Account/ChatRumi.Account.Api
dotnet run
```

Ensure the required infrastructure (PostgreSQL, Redis, etc.) is reachable and connection strings are configured in `appsettings.json` or via environment variables.

## Service Endpoints

| Service       | URL                        | Description                    |
|---------------|----------------------------|--------------------------------|
| Gateway       | http://localhost:7000      | Unified API entry point        |
| Account API   | http://localhost:5049      | Account management             |
| Chat API      | http://localhost:5111      | Messaging + SignalR hub        |
| Friendship API| http://localhost:5031      | Friend requests + SignalR hub  |
| Feed API      | http://localhost:5211      | Social feed & posts            |

### Infrastructure UIs

| Tool             | URL                        | Credentials            |
|------------------|----------------------------|------------------------|
| Consul           | http://localhost:8500      | —                      |
| RabbitMQ Manager | http://localhost:15672     | `admin` / `rbadminpass`|
| Kafka UI         | http://localhost:8082      | —                      |
| Kibana           | http://localhost:5601      | —                      |
| Jaeger           | http://localhost:16686     | —                      |

## Environment Variables

All configurable via `.env` (see `.env.example`):

| Variable                   | Description              | Default                          |
|----------------------------|--------------------------|----------------------------------|
| `DB_HOST` / `DB_PORT`     | PostgreSQL connection    | `host.docker.internal` / `5432`  |
| `DB_USER` / `DB_PASSWORD` | PostgreSQL credentials   | `postgres` / `postgres`          |
| `NEO4J_URI`               | Neo4j Bolt endpoint      | `bolt://host.docker.internal:7687` |
| `NEO4J_USER` / `NEO4J_PASSWORD` | Neo4j credentials  | `neo4j` / `test1234`            |
| `ELASTIC_URL`             | Elasticsearch endpoint   | `http://host.docker.internal:9200` |
| `RABBITMQ_HOST` / `RABBITMQ_PORT` | RabbitMQ connection | `host.docker.internal` / `5672` |
| `KAFKA_BOOTSTRAP_SERVERS` | Kafka broker address     | `host.docker.internal:9092`      |
| `CONSUL_HOST` / `CONSUL_PORT` | Consul connection    | `host.docker.internal` / `8500`  |
| `REDIS_HOST` / `REDIS_PORT` | Redis connection       | `host.docker.internal` / `6379`  |

## Design Patterns

- **Domain-Driven Design** — Bounded contexts per service with aggregates, entities, and value objects
- **CQRS** — Separate command and query models via MediatR
- **Event Sourcing** — Domain events stored as the source of truth in Marten
- **Event-Driven Architecture** — Kafka for cross-service event propagation, RabbitMQ for command-side messaging
- **API Gateway** — Single entry point with Ocelot routing, load balancing, and WebSocket passthrough
- **Service Discovery** — Runtime service registration and health monitoring with Consul

## License

This project is for personal/educational use.
