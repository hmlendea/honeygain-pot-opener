# Security

## Threat Model

### Assets

| Asset | Sensitivity | Location |
|-------|-------------|----------|
| Honeygain credentials (email + password) | High | `appsettings.json` (plain text), memory (BotSettings) |
| Honeygain session (cookies, tokens) | High | Browser profile (WebDriver) |
| Log file (contains email) | Medium | `logfile.log` |
| Configuration file | Medium | `appsettings.json` |

### Threat Actors

| Actor | Capability | Motivation |
|-------|------------|------------|
| Local user (same machine) | Read files, memory, process list | Credential theft |
| Malicious process (same user) | Memory scan, file access | Credential theft |
| Backup/Log aggregation system | Read log files | Credential exposure |
| Git repository | Commit history | Accidental credential commit |
| Network attacker | N/A (no network exposure) | N/A |

### Attack Surface

| Vector | Exposure | Mitigation |
|--------|----------|------------|
| `appsettings.json` | File read | File permissions, placeholder values |
| Process memory | `BotSettings` instance | Short-lived process |
| Log file | Email in structured logs | No password in logs |
| Browser profile | Session cookies | Ephemeral (cleaned on quit) |
| Command line | N/A (no args) | N/A |

## Current Security Controls

### 1. Credential Storage

**Current:** Plain text in `appsettings.json`

```json
{
  "botSettings": {
    "emailAddress": "user@example.com",
    "password": "actual-password"
  }
}
```

**Controls:**
- Repository uses placeholders: `[[HONEYGAIN_EMAIL_ADDRESS]]`, `[[HONEYGAIN_PASSWORD]]`
- Operator must replace with real values at deployment
- No encryption at rest

**Gaps:**
- No encryption
- No secret manager integration
- File permissions operator responsibility

### 2. Credential Handling in Memory

```csharp
// BotSettings registered as singleton
services.AddSingleton(botSettings);

// Injected into BotService → HoneygainProcessor
// Used only in:
processor.SetText(By.Id("email"), settings.EmailAddress);
processor.SetText(By.Id("password"), settings.Password);
```

**Controls:**
- Password never logged
- Password never serialized
- Short process lifetime (single run)

**Gaps:**
- Plain text in managed heap
- No `SecureString` usage
- No zeroing after use

### 3. Logging

**Controls:**
- Password never passed to logger
- Email logged as `Username` (intentional correlation)
- Exception messages don't include password (Selenium exceptions don't capture input values)

**Gaps:**
- Email in log file (PII)
- No log redaction framework
- Log file permissions operator responsibility

### 4. Browser Automation

**Controls:**
- WebDriver manages browser profile
- `driver.Quit()` in finally cleans session
- Headless mode default (no visible browser)

**Gaps:**
- Browser profile may persist cookies between runs (depends on WebDriverInitialiser)
- No explicit profile cleanup
- Selenium driver downloads not verified

### 5. Network

**Controls:**
- Only communicates with `dashboard.honeygain.com` (HTTPS)
- No custom certificate validation (uses system trust store)
- No proxy configuration

**Gaps:**
- No certificate pinning
- No request/response logging for audit
- No network segmentation

## Vulnerabilities & Mitigations

### VULN-001: Plain Text Credentials in Config File

| Aspect | Detail |
|--------|--------|
| **Severity** | High |
| **CVSS** | 7.5 (AV:L/AC:L/PR:N/UI:N/S:U/C:H/I:N/A:N) |
| **Location** | `appsettings.json` |
| **Impact** | Any local user/process can read Honeygain credentials |
| **Current Mitigation** | Placeholders in repo; operator sets file permissions |
| **Recommended Mitigation** | Environment variables, user secrets, or secret manager |

### VULN-002: Email in Log Files (PII)

| Aspect | Detail |
|--------|--------|
| **Severity** | Medium |
| **CVSS** | 4.3 (AV:L/AC:L/PR:N/UI:N/S:U/C:L/I:N/A:N) |
| **Location** | `logfile.log` (structured logs) |
| **Impact** | Email exposed in log aggregation/backup systems |
| **Current Mitigation** | None (intentional for correlation) |
| **Recommended Mitigation** | Hash email in logs, or use correlation ID instead |

### VULN-003: No Startup Validation

| Aspect | Detail |
|--------|--------|
| **Severity** | Medium |
| **CVSS** | 5.3 (AV:L/AC:L/PR:N/UI:N/S:U/C:N/I:L/A:L) |
| **Location** | `Program.cs` config binding |
| **Impact** | Missing/invalid config causes runtime failures with stack traces |
| **Current Mitigation** | None |
| **Recommended Mitigation** | Validate at startup, fail fast with clear message |

### VULN-004: No Retry/Recovery on Transient Failures

| Aspect | Detail |
|--------|--------|
| **Severity** | Low |
| **CVSS** | 3.1 (AV:L/AC:H/PR:N/UI:N/S:U/C:N/I:N/A:L) |
| **Location** | `BotService.Run()` |
| **Impact** | Transient network/Selenium issues cause operation failure |
| **Current Mitigation** | External scheduler re-invokes |
| **Recommended Mitigation** | Configurable retry with backoff |

### VULN-005: Fragile Selectors (XPath/ID)

| Aspect | Detail |
|--------|--------|
| **Severity** | Low |
| **CVSS** | 2.7 (AV:N/AC:H/PR:N/UI:N/S:U/C:N/I:N/A:L) |
| **Location** | `HoneygainProcessor.cs` |
| **Impact** | Honeygain UI changes break automation |
| **Current Mitigation** | None |
| **Recommended Mitigation** | More robust selectors, version detection |

## Secure Deployment Checklist

### Pre-Deployment

- [ ] `appsettings.json` not committed with real credentials
- [ ] Placeholder values in repository: `[[HONEYGAIN_EMAIL_ADDRESS]]`, `[[HONEYGAIN_PASSWORD]]`
- [ ] Deployment process replaces placeholders with real values
- [ ] File permissions: `chmod 600 appsettings.json`
- [ ] Log directory permissions: `chmod 700 logs/` (if separate)

### Runtime

- [ ] Process runs under dedicated service account (not root/admin)
- [ ] No other users can read process memory (standard OS isolation)
- [ ] Log file monitored for size/rotation
- [ ] External scheduler (cron, systemd timer, Task Scheduler) configured

### Post-Deployment

- [ ] Verify log file contains no passwords
- [ ] Verify email in logs is acceptable for compliance
- [ ] Monitor for failed runs (exit code, log alerts)
- [ ] Rotate Honeygain password periodically

## Compliance Considerations

### GDPR / Personal Data

| Data | Category | Processing Basis | Retention |
|------|----------|------------------|-----------|
| Email | Personal (direct identifier) | Legitimate interest (automation) | Log retention period |
| Password | Special category (auth secret) | Not processed (only used) | N/A |
| Session cookies | Personal (indirect identifier) | Legitimate interest | Session only |

**Data Subject Rights:**
- Access: Email in logs → extractable
- Rectification: Update `appsettings.json`
- Erasure: Delete `appsettings.json`, `logfile.log`
- Portability: Not applicable

### Secrets Management Standards

| Standard | Current | Target |
|----------|---------|--------|
| NIST SP 800-53 (IA-5) | Fail (plain text) | Use secret manager |
| OWASP ASVS 7.1 | Fail (no encryption) | Encrypt at rest |
| PCI DSS 8.2 | Fail (plain text) | Never store in config |

## Security Testing

### Static Analysis

```bash
# Check for hardcoded secrets
grep -r "password\|secret\|key" --include="*.cs" --include="*.json" .

# Check for logging of sensitive data
grep -r "logger\." --include="*.cs" . | grep -i "password\|secret"
```

### Runtime Verification

```bash
# Verify no password in logs
grep -i password logfile.log

# Verify email in logs (expected)
grep "Username" logfile.log

# Verify file permissions
ls -la appsettings.json logfile.log
```

### Penetration Testing Scope

- Local privilege escalation via config file read
- Log file exposure via backup systems
- Memory scraping for credentials
- Browser profile persistence

## Incident Response

### Credential Compromise

1. **Immediate:** Rotate Honeygain password
2. **Investigate:** Check log access, file permissions, backup systems
3. **Remediate:** Implement secret manager, rotate all credentials
4. **Verify:** Confirm no password in logs, config secured

### Log Exposure

1. **Assess:** Which logs exposed, to whom, for how long
2. **Notify:** If email considered PII under applicable law
3. **Remediate:** Implement log redaction, reduce log retention
4. **Verify:** New logs don't contain email (or hashed)

## Future Security Improvements

| Priority | Improvement | Effort |
|----------|-------------|--------|
| High | Environment variable config source | Low |
| High | User secrets for development | Low |
| High | Secret manager integration (Azure Key Vault, HashiCorp Vault) | Medium |
| Medium | Email hashing in logs (SHA-256 with salt) | Low |
| Medium | Startup config validation | Low |
| Medium | SecureString for password in memory | Medium |
| Low | Certificate pinning for Honeygain API | Medium |
| Low | Request/response audit logging | Medium |
| Low | Automated dependency vulnerability scanning | Low |

## Related Documentation

- [Configuration](./configuration.md)
- [Logging](./logging.md)
- [Privacy](../PRIVACY.md)
- [Error Handling](./error-handling.md)
- [Components: Host and Composition](./components/host-and-composition.md)