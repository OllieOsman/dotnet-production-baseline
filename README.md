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
src/
Api/
Program.cs
Startup.cs
Health/
Middleware/
Infrastructure/
Logging/
Resilience/
Hosting/
Workers/
BackgroundServices/

The structure favors **behavioral boundaries** over technical layers.

---

## Startup philosophy

Startup should be:
- explicit
- fast
- fail early

If a service cannot start safely, it **should not start at all**.

There are no “best effort” startups here.

---

## Shutdown philosophy

Shutdown should be:
- cooperative
- observable
- boring

When a SIGTERM is received:
1. Stop accepting new work
2. Allow in-flight work to complete
3. Respect time limits
4. Exit cleanly

If work cannot finish safely, it should **fail loudly**, not silently.

---

## Health checks

This baseline separates:
- **Liveness**: _“Is the process running?”_
- **Readiness**: _“Can this instance safely receive traffic?”_

Readiness reflects:
- dependency availability
- background worker state
- startup completion

If readiness is false, traffic should be routed elsewhere.

---

## Resilience defaults

Retries are:
- bounded
- intentional
- observable

Timeouts are:
- explicit
- enforced
- short by default

This baseline assumes:
> _Retries are a form of load._

They must be treated as such.

---

## Configuration rules

- Invalid configuration fails fast
- Required values are validated at startup
- Runtime reloads are opt-in
- Secrets are never logged

Configuration drift is treated as a production risk.

---

## What this does **not** guarantee

This baseline does not make your system:
- scalable
- fast
- correct
- cheap

It makes your system:
- predictable
- debuggable
- stoppable
- safer to evolve

---

## How to use this repository

You can:
- clone it as a starting point
- copy pieces selectively
- disagree with it (recommended)

If you disagree, document **why** — that’s where learning happens.

---

## Roadmap

This repository evolves slowly and intentionally.

Planned areas of exploration:
- Backpressure strategies
- Deployment-aware behavior
- Graceful degradation patterns
- Dependency failure modes

---

## Philosophy

> Most production failures are not bugs —  
> they are the system doing exactly what it was allowed to do.

This repository narrows what a service is allowed to do by default.
