# ChatRumi Docker Setup
## Quick Start
### Full Stack (All in Docker)
```bash
docker compose up -d
```
### External Infrastructure Mode
1. Copy .env.example to .env
2. Edit .env and set your external host addresses
3. Start only API services:
```bash
docker compose up -d account-service chat-service friendship-service feed-service gateway
```
## Services
**APIs:**
- Gateway: http://localhost:7000
- Account: http://localhost:5049  
- Chat: http://localhost:5111
- Friendship: http://localhost:5031
- Feed: http://localhost:5211
**Infrastructure:**
- Consul: http://localhost:8500
- Kibana: http://localhost:5601
- RabbitMQ: http://localhost:15672 (admin/rbadminpass)
- Kafka UI: http://localhost:8082
## Environment Variables
See .env.example for all available environment variables.
Use host.docker.internal on Windows/Mac for external services.

**MassTransit license:** MassTransit v9 is commercial, so account-service will not start its RabbitMQ bus without a license.
Set `MT_LICENSE` (license text) in `.env` or your shell. When running through Aspire, set it as a Host user secret instead:
```bash
dotnet user-secrets set "Parameters:masstransit-license" "<license>" --project src/ChatRumi.Host
```
Integration tests use the in-memory transport and need no license.
