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
