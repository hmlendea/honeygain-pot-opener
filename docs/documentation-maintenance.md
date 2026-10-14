# Documentation Maintenance

## Documentation Structure

```
docs/
├── INDEX.md                          # This file - documentation index
├── architecture.md                   # System architecture
├── repository-overview.md            # Repository purpose and scope
├── repository-structure.md           # File/folder structure
├── design-decisions.md               # Key architectural decisions
├── configuration.md                  # Configuration schema and behaviour
├── logging.md                        # Logging framework and practices
├── error-handling.md                 # Error taxonomy and handling
├── security.md                       # Security model and threats
├── dependencies.md                   # Dependency inventory
├── build-and-deployment.md           # Build and deployment guide
├── testing.md                        # Test strategy and conventions
├── change-guide.md                   # Versioning and change process
├── concurrency-and-scheduling.md     # Concurrency model
├── data-model.md                     # Data structures and flow
├── documentation-maintenance.md      # This file
├── faq.md                            # Frequently asked questions
├── invariants.md                     # System invariants
├── integrations.md                   # External integrations
├── quick-start.md                    # Quick start guide
├── state-and-persistence.md          # State management
├── troubleshooting.md                # Common issues and fixes
├── api-usage-examples.md             # API usage examples
├── ambiguities-and-open-questions.md # Known ambiguities
├── components/
│   ├── host-and-composition.md       # Composition root and DI
│   ├── application-services.md       # BotService, IBotService
│   └── integration-models.md         # HoneygainProcessor, IWebProcessor
├── flows/
│   ├── startup-and-rendering.md      # Startup sequence
│   ├── authentication.md             # Login flow
│   ├── reward-claim.md               # Claim reward flow
│   └── lucky-pot.md                  # Lucky Pot flow
├── behaviour/
│   ├── browse-and-search.md          # Navigation behaviour
│   └── inspect-edit-delete.md        # Configuration behaviour
└── api-reference/
    └── INDEX.md                      # Internal service contracts
```

## Maintenance Principles

### 1. Documentation as Code

- Documentation lives in `docs/` alongside source
- Versioned with code (same commit)
- Reviewed in PRs with code changes
- Built/validated in CI (link checking, markdown lint)

### 2. Single Source of Truth

| Information | Authoritative Source |
|-------------|---------------------|
| Architecture | `architecture.md` |
| Config schema | `configuration.md` |
| API contracts | `api-reference/INDEX.md` |
| Error handling | `error-handling.md` |
| Security | `security.md` |
| Dependencies | `dependencies.md` |

**No duplication** — reference other docs via relative links.

### 3. Update Triggers

Update documentation when:

| Trigger | Docs to Update |
|---------|----------------|
| Config change | `configuration.md`, `README.md` |
| New operation/flow | `flows/*.md`, `api-reference/INDEX.md` |
| Selector change | `components/integration-models.md`, `flows/*.md` |
| Logging change | `logging.md`, `api-reference/INDEX.md` |
| Error handling change | `error-handling.md` |
| Security change | `security.md`, `PRIVACY.md` |
| Dependency change | `dependencies.md`, `build-and-deployment.md` |
| Breaking change | `change-guide.md`, `CHANGELOG.md` |
| New invariant | `invariants.md` |

## Writing Standards

### Markdown Conventions

- **Headings:** Sentence case (`## Configuration schema`, not `## CONFIGURATION SCHEMA`)
- **Code symbols:** Backticks (`BotSettings`, `IWebProcessor`, `appsettings.json`)
- **File paths:** Relative, backticked (`docs/configuration.md`)
- **Links:** Relative, lowercase (`./configuration.md`)
- **Tables:** For structured data (config keys, error types, etc.)
- **Code blocks:** Language-annotated (`csharp`, `json`, `bash`, `yaml`)

### Content Standards

- **Evidence-based:** Every claim traceable to source code
- **No speculation:** "Future:" or "Planned:" labels for unimplemented features
- **Current state:** Document what exists, not what should exist
- **Cross-references:** Link to related docs liberally

### File Naming

- **kebab-case:** `build-and-deployment.md`
- **Descriptive:** `error-handling.md` not `errors.md`
- **Consistent:** `flows/authentication.md` not `flows/login.md`

## Validation

### Automated Checks (CI)

```yaml
# .github/workflows/docs.yml
- name: Check links
  run: |
    npm install -g markdown-link-check
    markdown-link-check docs/**/*.md

- name: Lint markdown
  run: |
    npm install -g markdownlint-cli
    markdownlint docs/**/*.md
```

### Manual Checks

- [ ] All relative links resolve
- [ ] Code snippets compile (copy-paste test)
- [ ] Config examples match actual schema
- [ ] Version numbers consistent
- [ ] No broken external links

## Review Process

### PR Requirements

- [ ] Documentation updated for any user-facing change
- [ ] New files follow structure and naming conventions
- [ ] Links verified
- [ ] No markdown lint errors

### Periodic Audit (Quarterly)

1. Check for outdated content (compare to code)
2. Verify all external links still work
3. Review FAQ for new common questions
4. Update version references
5. Check for missing cross-references

## Tooling

### Recommended

| Tool | Purpose |
|------|---------|
| `markdown-link-check` | Validate links |
| `markdownlint` | Style consistency |
| `vale` | Prose style (optional) |
| `cspell` | Spell check |

### VS Code Extensions

- Markdown All in One
- markdownlint
- Link Checker

## Adding New Documentation

### New Component Doc

1. Create in appropriate subdirectory (`components/`, `flows/`, `behaviour/`)
2. Follow existing template (see `components/host-and-composition.md`)
3. Add entry to `INDEX.md` table
4. Cross-reference from related docs

### New Cross-Cutting Doc

1. Create in `docs/` root
2. Follow structure: Overview → Details → Related
3. Add to `INDEX.md`
4. Link from `README.md` if user-facing

## Deprecating Documentation

1. Move to `docs/archive/` with date prefix: `archive/2026-01-old-doc.md`
2. Update links to point to replacement
3. Add notice at top of archived file
4. Remove from `INDEX.md`

## Related Documentation

- [Repository Structure](./repository-structure.md)
- [Architecture](./architecture.md)
- [Change Guide](./change-guide.md)
- [README](../README.md)