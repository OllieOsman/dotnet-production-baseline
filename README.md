# dotnet-production-baseline

An opinionated baseline for building **ASP.NET Core services that behave well in production**.

This is not a starter template for learning .NET.
It’s a reference for **how I expect a service to act once it’s deployed**.

The goal is boring reliability:
- predictable startup
- graceful shutdown
- bounded retries
- observable behavior
- safe defaults

---

## Why this exists

Most production issues don’t come from missing features —  
they come from **missing behavior**:

- services that don’t shut down cleanly
- retries that amplify failures
- background work that gets killed mid-flight
- logs that can’t be correlated
- health checks that lie

This repository exists to answer:
> _“What should a service do by default if it’s going to run in production?”_

---

## Non-goals

This repository intentionally avoids:
- frontend concerns
- auth / identity
- business logic examples
- framework comparisons
- performance micro-optimizations

Those distract from the core problem: **service behavior**.

---

## What this baseline includes

### Application lifecycle
- Explicit startup phases
- Graceful shutdown handling
- Cooperative cancellation
- Background worker coordination

### Health & readiness
- Liveness vs readiness checks
- Dependency-aware readiness
- Startup and shutdown signaling

### Reliability defaults
- Timeouts everywhere
- Bounded retries
- Circuit breakers where appropriate
- No unbounded queues

### Observability
- Structured logging
- Correlation IDs
- Clear log levels
- Metrics-friendly design

### Configuration
- Layered configuration
- Environment separation
- Runtime-safe reloads
- Explicit failure on bad config

---

## Project structure

