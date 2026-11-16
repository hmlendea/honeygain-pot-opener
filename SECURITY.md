# Security Policy

This policy defines responsible vulnerability reporting for the current GitHub Release of Honeygain Pot Opener and supports private, coordinated remediation.

## 📑 Table of Contents

- [Table of Contents](#table-of-contents)
- [Supported Versions](#supported-versions)
- [Reporting a Vulnerability](#reporting-a-vulnerability)
- [Scope](#scope)
- [Disclosure Policy](#disclosure-policy)
- [Automated Security Scanning](#automated-security-scanning)
- [Safe Harbour](#safe-harbour)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| Latest version | GitHub Releases | ✅ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/honeygain-pot-opener/security/advisories)
- Contact the maintainers directly

## 📌 Scope

The original maintainers are not responsible for changes, modifications, or security issues introduced by downstream forks.

The subsequent report categories are in scope for this repository:
- Exposure, insecure handling, or logging of Honeygain credentials and account information.
- Defects in configuration, browser automation, dependencies, or release artefacts that permit unauthorised code execution, data disclosure, or account compromise.

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Vulnerabilities in Honeygain services, infrastructure, or account policies that are not caused by this repository.
- Security matters in unsupported releases, unofficial third-party distributions, browsers, WebDrivers, or third-party packages without a repository-specific defect.

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.

## 🔍 Automated Security Scanning

This repository runs automated security scans on every push and pull request to `master`, and on a weekly schedule. The scan includes:
- **Dependency vulnerability scanning** via `dotnet list package --vulnerable --include-transitive`
- **CodeQL static analysis** for C# code

Results are published to the [Security](https://github.com/hmlendea/honeygain-pot-opener/security) tab and the [Actions](https://github.com/hmlendea/honeygain-pot-opener/actions/workflows/security.yml) workflow page.

## 🧾 Safe Harbour

If your research is conducted in good faith, confined to authorised scope, and disclosed responsibly, the maintainers will not pursue action for policy-compliant activity.