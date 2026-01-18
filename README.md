# dotnet-production-baseline

An opinionated baseline for building **production-ready .NET services**: health checks, logging, config, containerization, and deploy-friendly defaults.

## What’s included

### Today
- Minimal ASP.NET Core API
- Liveness + readiness health endpoints
- Dockerfile + docker-compose for local runs
- Consistent repo conventions (.editorconfig, structure)

### Planned (iterative)
- Structured logging + correlation IDs
- OpenTelemetry (metrics/traces) + sample dashboard stack
- Exception handling + problem details
- Versioning strategy + build metadata
- CI pipeline + code quality gates
- Resilience policies (timeouts/retries/circuit breaker)
- Security baseline (headers, auth sample, secrets guidance)

## Quickstart

### Run with Docker
```bash
docker compose up --build

## Graceful shutdown & readiness

When the application receives a shutdown signal:
1. Readiness immediately switches to unhealthy
2. Traffic should stop being routed to the instance
3. In-flight requests are allowed to complete

## Observability

All logs are automatically enriched with:
correlation_id
trace_id
span_id
Traces are emitted via OpenTelemetry
Outbound calls and retries are traced and logged

