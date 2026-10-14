# Honeygain Pot Opener Documentation

## Repository Summary

Honeygain Pot Opener is a .NET 10.0 console application that automates a single Honeygain dashboard session per invocation. It authenticates with configured credentials, claims an available achievement reward, attempts to open the daily Lucky Pot, records structured operation events through NuciLog, and terminates the browser session. The application has no scheduler, database, or network listener; it is designed to be invoked by an external operator or scheduler.

## Root Document Map

- [ARCHITECTURE.md](../ARCHITECTURE.md) — High-level architecture, components, runtime flow, and design constraints
- [SECURITY.md](../SECURITY.md) — Security policy, supported versions, vulnerability reporting, and scope
- [PRIVACY.md](../PRIVACY.md) — Personal data handling, storage, retention, external integrations, and user controls
- [CONTRIBUTING.md](../CONTRIBUTING.md) — Contribution guidelines, code style, testing, and pull request process
- [LICENSE](../LICENSE) — MIT License
- [ROADMAP.md](../ROADMAP.md) — Project roadmap (if present)

## Documentation Catalogue

### Architecture & Design
| File | Description |
|------|-------------|
| [architecture.md](./architecture.md) | Detailed architecture elaboration complementing root ARCHITECTURE.md |
| [design-decisions.md](./design-decisions.md) | Key architectural and design choices with rationale |
| [repository-overview.md](./repository-overview.md) | Repository purpose, scope, and entry points |
| [repository-structure.md](./repository-structure.md) | Source tree layout and module organisation |

### Components
| File | Description |
|------|-------------|
| [components/host-and-composition.md](./components/host-and-composition.md) | Program.cs composition root, DI container, service registration, and startup |
| [components/application-services.md](./components/application-services.md) | BotService orchestration, operation ordering, and failure continuation |
| [components/integration-models.md](./components/integration-models.md) | HoneygainProcessor, IWebProcessor, SeleniumWebProcessor, and browser automation |

### Execution Flows
| File | Description |
|------|-------------|
| [flows/startup-and-rendering.md](./flows/startup-and-rendering.md) | Process startup, configuration loading, WebDriver initialisation, and service composition |
| [flows/authentication.md](./flows/authentication.md) | Login flow: navigation, cookie consent, credential submission, dashboard verification |
| [flows/reward-claim.md](./flows/reward-claim.md) | Achievement reward detection and claim flow |
| [flows/lucky-pot.md](./flows/lucky-pot.md) | Lucky Pot detection and opening flow |

### Behaviour
| File | Description |
|------|-------------|
| [behaviour/browse-and-search.md](./behaviour/browse-and-search.md) | Browser navigation, element location, and interaction patterns |
| [behaviour/inspect-edit-delete.md](./behaviour/inspect-edit-delete.md) | Configuration binding, settings objects, and runtime modification constraints |

### Cross-Cutting Concerns
| File | Description |
|------|-------------|
| [configuration.md](./configuration.md) | Configuration schema, sources, precedence, and reload behaviour |
| [logging.md](./logging.md) | NuciLog framework, levels, structured fields, correlation, sinks, redaction, retention |
| [error-handling.md](./error-handling.md) | Error taxonomy, handling patterns, recovery, and continuation semantics |
| [security.md](./security.md) | Security model, threats, mitigations (complements root SECURITY.md) |
| [concurrency-and-scheduling.md](./concurrency-and-scheduling.md) | Threading model, async boundaries, resource ownership, and concurrency guarantees |
| [state-and-persistence.md](./state-and-persistence.md) | State management, configuration persistence, log storage, browser session lifecycle |
| [dependencies.md](./dependencies.md) | External and internal dependency inventory with integration boundaries |
| [invariants.md](./invariants.md) | System-wide invariants and contracts |
| [integrations.md](./integrations.md) | External system integrations (Honeygain dashboard, browser, WebDriver) |

### Reference & Operations
| File | Description |
|------|-------------|
| [api-reference/INDEX.md](./api-reference/INDEX.md) | API reference index (no HTTP controllers; documents internal service contracts) |
| [api-usage-examples.md](./api-usage-examples.md) | Example invocations, configuration, and log output |
| [build-and-deployment.md](./build-and-deployment.md) | Build pipeline, release script, deployment artefacts, and environments |
| [testing.md](./testing.md) | Test strategy, organisation, coverage, and conventions |
| [change-guide.md](./change-guide.md) | How to modify common areas safely (selectors, settings, operations) |
| [troubleshooting.md](./troubleshooting.md) | Common issues and solutions |
| [faq.md](./faq.md) | Frequently asked questions |
| [documentation-maintenance.md](./documentation-maintenance.md) | How to keep docs current, ownership, and review cadence |
| [ambiguities-and-open-questions.md](./ambiguities-and-open-questions.md) | Unresolved items, TODOs, known gaps |
| [quick-start.md](./quick-start.md) | Getting started guide for newcomers |

## Navigation Aids

- **Start here (newcomers):** [quick-start.md](./quick-start.md) → [repository-overview.md](./repository-overview.md) → [flows/startup-and-rendering.md](./flows/startup-and-rendering.md)
- **Deep dive (component owners):** [components/](./components/) → [architecture.md](./architecture.md) → [design-decisions.md](./design-decisions.md)
- **Flows (debuggers):** [flows/](./flows/) → [error-handling.md](./error-handling.md) → [logging.md](./logging.md)

## Maintenance Metadata

- **Last reviewed:** 2026-10-14
- **Owner:** hmlendea
- **Coverage status:** Complete for current codebase (v1.1.0+)