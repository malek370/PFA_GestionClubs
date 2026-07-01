# Agent Instructions

## Project Overview

University club management platform (PFA) with three microservices communicating via Kafka:

| Service | Tech | Port | Purpose |
|---------|------|------|---------|
| **GestionClubs** | .NET 9, Clean Architecture | 8081 (HTTPS), 8082 (HTTP) | Club/member/event/adhesion management |
| **IdentityProvider** | .NET 9, ASP.NET Identity | 8443 (HTTPS), 8080 (HTTP) | Auth (JWT RSA), registration, token refresh |
| **ChatbotService** | Python, Django | — | NLP chatbot for club information |

## Architecture

### GestionClubs — Clean Architecture Layers

```
Domain/          → Entities, Enums, Pagination (no framework dependencies)
Application/     → Services, IRepositories, IServices, Exceptions, Events
Infrastructure/  → EF Core DbContext, Kafka producers/consumers, Decorators
GestionClubs/    → API layer (Controllers, Validators, Handlers, Program.cs)
```

**Key patterns:**
- **Decorator pattern** for Kafka event publishing — services are wrapped with `*KafkaDecorator` classes registered in DI
- **Repository pattern** via `IBaseRepository<T>` generic interface
- **Minimal API-style controllers** using static extension methods (`AddEndpoints`)
- **Global exception handler** (`GlobalExcpectionHandler`)
- JWT validation against IdentityProvider's public keys (JWKS)

### Kafka Event Flows

| Event | Producer | Consumer | Topic |
|-------|----------|----------|-------|
| UserRegistered | IdentityProvider | GestionClubs | `user-registered` |
| UserPromotedToClubAdmin | GestionClubs | IdentityProvider | `user-promoted-to-club-admin` |

Configuration via `KafkaOptions` bound from `appsettings.json` section `"Kafka"`.

### Domain Entities

`Club`, `Member`, `User`, `Adhesion`, `Annoucement`, `Event`, `AppRoles` — all inherit `BaseEntity`.

## Build & Run

### Docker (full stack)

```bash
# Generate HTTPS certs first (see README.md)
docker compose up -d --pull always
```

Services: Kafka (KRaft), SQL Server (×2: ports 1433, 1434), IdentityProvider, GestionClubs.

### .NET services (local dev)

```bash
# GestionClubs
cd GestionClubs/GestionClubs
dotnet run

# IdentityProvider
cd IdentityProvider
dotnet run
```

### Tests

```bash
# Unit tests (Application layer, uses Moq + InMemory EF)
cd GestionClubs
dotnet test Application.Test/

# Domain tests
dotnet test Domain.Test/

# Integration tests (WebApplicationFactory + InMemory DB, xUnit)
dotnet test Integration.Test/
```

### ChatbotService (Django)

```bash
cd ChatbotService/Chatbot
python -m venv .venv
pip install -r requirements.txt
python manage.py runserver
```

## Conventions

- **Target framework:** .NET 9, C# 13, nullable enabled
- **Testing:** xUnit + Moq + MockQueryable + `Microsoft.AspNetCore.Mvc.Testing`
- **ORM:** Entity Framework Core with SQL Server
- **API docs:** OpenAPI via Scalar (`/scalar/v1`)
- **Kafka client:** Confluent.Kafka with manual `IHostedService` consumers
- **Auth:** JWT Bearer with RSA asymmetric keys (public key fetched from IdP JWKS endpoint)
- **Validation:** FluentValidation-style validators in `Validators/` folder

## Known Issues (from [ARCHITECTURE_REVIEW.md](GestionClubs/ARCHITECTURE_REVIEW.md))

- DTOs currently in Domain layer — should be in Application layer
- Some Clean Architecture dependency rule violations
- N+1 query risks in service layer

## Important Files

- [docker-compose.yml](docker-compose.yml) — Full infrastructure definition
- [KAFKA_INTEGRATION_PLAN.md](KAFKA_INTEGRATION_PLAN.md) — Kafka design decisions
- [GestionClubs/GestionClubs/Program.cs](GestionClubs/GestionClubs/Program.cs) — API DI composition root
- [IdentityProvider/Program.cs](IdentityProvider/Program.cs) — IdP DI composition root
- [seedGestionClub.sql](seedGestionClub.sql) — Database seed script
