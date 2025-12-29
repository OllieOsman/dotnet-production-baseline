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
