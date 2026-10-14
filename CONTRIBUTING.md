# Contributing to Honeygain Pot Opener

This document covers the guidelines and processes for contributing to this project, including how to report issues, suggest enhancements, submit code changes, and follow project standards.

## 📑 Table of Contents

- [How to Contribute](#how-to-contribute)
  - [Reporting Issues](#reporting-issues)
  - [Suggesting Enhancements](#suggesting-enhancements)
  - [Code Contributions](#code-contributions)
    - [Prerequisites](#prerequisites)
    - [Development Setup](#development-setup)
    - [Making Changes](#making-changes)
    - [Pull Request Guidelines](#pull-request-guidelines)
  - [Code Style](#code-style)
  - [Testing](#testing)
  - [Documentation](#documentation)
- [Code of Conduct](#code-of-conduct)
- [Security](#security)
- [License](#license)
- [Recognition](#recognition)
- [Getting Help](#getting-help)

## 🤝 How to Contribute

### Reporting Issues

- Search existing issues first.
- Use the issue templates if available.
- Provide clear reproduction steps.
- Include environment details.

### Suggesting Enhancements

- Check the roadmap and existing discussions.
- Explain the use case and expected behavior.
- Consider implementation complexity.

### Code Contributions

#### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A Honeygain account
- A Selenium-compatible browser and corresponding WebDriver

#### Development Setup

```bash
# Clone the repository
git clone https://github.com/hmlendea/honeygain-pot-opener.git
cd honeygain-pot-opener

# Install dependencies
dotnet restore
```

#### Making Changes

1. Fork the repository.
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes.
4. Run tests: `dotnet test`
5. Run linters: `dotnet build --no-restore`
6. Commit with clear and descriptive messages.
7. Push to your fork.
8. Open a Pull Request.

#### Pull Request Guidelines

- Target the `master` branch.
- Keep PRs focused and atomic.
- Update documentation if applicable.
- Add tests for any new functionality.
- Ensure the CI checks pass.

### Code Style

Follow the project's coding standards:
- Explicit types (no `var`)
- `GivenX_WhenY_ThenZ` test naming convention
- Private fields: camelCase without underscore prefix
- Null-forgiving operator for test fields
- OperationStatus comparisons via `.Name` property
- Moq verification with `It.Is<Operation>(o => o.Name == "...")`
- Run `dotnet build --no-restore` before committing.

### Testing

```bash
# Run all tests
dotnet test

# Run specific test suite
dotnet test HoneygainPotOpener.UnitTests/HoneygainPotOpener.UnitTests.csproj
```

### Documentation

- Update relevant docs for changes.
- Follow the documentation style guide.
- Preview changes locally if possible.

## 🔒 Security

Report security vulnerabilities per the [Security Policy](./SECURITY.md).

## 📄 License

By contributing, you agree that your contributions will be licensed under the GNU General Public License v3.0.

## 🙏 Recognition

Contributors are recognized in the GitHub contributors list.

## ❓ Getting Help

- Open an issue on GitHub
- Check existing discussions and documentation first.