# Troubleshooting

## Common Issues

### 1. "NoSuchElementException" / Element Not Found

**Symptoms:**
```
OpenQA.Selenium.NoSuchElementException: no such element: Unable to locate element: {"method":"xpath","selector":"//button[contains(text(), 'Claim reward')]"}
```

**Causes:**
- Honeygain UI changed (new HTML structure, different text)
- Page not fully loaded
- Element in different frame/iframe

**Fixes:**
1. Run with `debugSettings.isDebugMode: true` to watch browser
2. Inspect element in browser DevTools
3. Update selector constant in `HoneygainProcessor.cs`:
   - `ClaimRewardButtonXPath`
   - `OpenLuckyPotButtonXPath`
   - `DashboardIndicatorXPath`
4. Run tests: `dotnet test`

### 2. "WebDriverTimeoutException" / Timeout Waiting for Element

**Symptoms:**
```
OpenQA.Selenium.WebDriverTimeoutException: Timed out after 10 seconds waiting for element to be visible
```

**Causes:**
- Slow network / page load
- Element selector changed
- Page structure changed (element not present)
- Chrome/Chromium not installed or wrong version

**Fixes:**
1. Check Chrome installed: `chromium-browser --version` or `chrome --version`
2. Increase timeout in `HoneygainProcessor` (modify `WaitForElementToBeVisible` calls)
3. Verify network access to `dashboard.honeygain.com`
4. Run with `isDebugMode: true` to observe

### 3. "InvalidOperationException: There is no reward available"

**Symptoms:**
```
System.InvalidOperationException: There is no reward available to claim at this time.
```

**Cause:** **Normal behaviour** — no daily reward available today.

**Log Level:** `Warning` (expected condition)

**Action:** None needed. Runs again tomorrow.

### 4. "InvalidOperationException: The Lucky Pot cannot be opened"

**Symptoms:**
```
System.InvalidOperationException: The Lucky Pot cannot be opened at this time.
```

**Cause:** **Normal behaviour** — pot already opened or not ready.

**Log Level:** `Error` (primary objective failed)

**Action:** None needed. Runs again tomorrow.

### 5. "Fatal" in Log File

**Symptoms:** Log contains `Fatal` level entries.

**Causes:**
- Malformed `appsettings.json` (invalid JSON)
- Chrome/Chromium not installed
- WebDriver version mismatch
- Permission denied on config/log file
- .NET runtime not found (framework-dependent publish)

**Fixes:**
1. Check `appsettings.json` syntax: `cat appsettings.json | jq .`
2. Install Chrome/Chromium
3. Use self-contained publish: `dotnet publish -r linux-x64 --self-contained`
4. Check file permissions: `chmod 600 appsettings.json`

### 6. Process Exits Immediately / No Log Output

**Symptoms:** No `logfile.log` created, process exits instantly.

**Causes:**
- `appsettings.json` missing or unreadable
- `nuciLoggerSettings.isFileOutputEnabled: false`
- Permission denied on log directory

**Fixes:**
1. Verify `appsettings.json` exists in working directory
2. Check `isFileOutputEnabled: true`
3. Ensure write permission on working directory

### 7. "ElementClickInterceptedException" / Click Intercepted

**Symptoms:**
```
OpenQA.Selenium.ElementClickInterceptedException: element click intercepted
```

**Causes:**
- Cookie banner / modal overlaying button
- Animation in progress
- Element not fully rendered

**Fixes:**
1. Increase wait time before click
2. Add explicit wait for overlay to disappear
3. Update selector to target correct element

### 8. "StaleElementReferenceException"

**Symptoms:**
```
OpenQA.Selenium.StaleElementReferenceException: stale element reference
```

**Cause:** DOM updated between finding element and interacting.

**Fix:** Re-find element before interaction (handled by `IWebProcessor` methods).

## Diagnostic Steps

### 1. Enable Debug Mode

```json
{
  "debugSettings": {
    "isDebugMode": true
  }
}
```
- Browser stays visible
- Watch each step
- Inspect elements manually

### 2. Increase Log Level

```json
{
  "nuciLoggerSettings": {
    "minimumLevel": "Debug"
  }
}
```
- More detailed NuciLog output
- See all Selenium commands

### 3. Run Manually Once

```bash
./HoneygainPotOpener
```
- Observe console output
- Check `logfile.log` after

### 4. Verify Chrome/Chromium

```bash
# Linux
chromium-browser --version
google-chrome --version

# Windows
chrome.exe --version

# macOS
/Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome --version
```

### 5. Test Network Connectivity

```bash
curl -I https://dashboard.honeygain.com/login
# Should return 200 OK
```

### 6. Check File Permissions

```bash
ls -la appsettings.json logfile.log
# appsettings.json: 600 (owner read/write)
# logfile.log: 644 or 600
```

## Environment-Specific Issues

### Linux (Headless Server)

| Issue | Solution |
|-------|----------|
| No display | Use `isDebugMode: false` (headless) — default |
| Missing libraries | Install: `libgtk-3-0 libnss3 libasound2 libxss1 libgconf-2-4` |
| Chrome not found | Install `chromium-browser` or `google-chrome-stable` |
| Sandbox issues | Add `--no-sandbox` (WebDriverInitialiser handles) |

### Windows

| Issue | Solution |
|-------|----------|
| WebDriver not found | WebDriverInitialiser auto-downloads |
| Chrome not in PATH | Install Chrome for all users |
| Antivirus blocks driver | Allow `chromedriver.exe` |

### Docker

| Issue | Solution |
|-------|----------|
| Chrome not in container | Install in Dockerfile: `apt-get install -y chromium` |
| Shared memory | Add `--shm-size=2g` to `docker run` |
| Root user | Run as non-root user |

## Log Analysis

### Success Pattern (Expected)

```
Operation=StartUp Status=Success
Operation=LogIn Status=Started Username="user@example.com"
Operation=LogIn Status=Success Username="user@example.com"
Operation=ClaimReward Status=Started Username="user@example.com"
Operation=ClaimReward Status=Success Username="user@example.com"
Operation=OpenPot Status=Started Username="user@example.com"
Operation=OpenPot Status=Success Username="user@example.com"
```

### Warning Pattern (No Reward)

```
Operation=ClaimReward Status=Started Username="user@example.com"
Operation=ClaimReward Status=Failure Username="user@example.com" Exception="InvalidOperationException: There is no reward available..."
Operation=ClaimReward Status=Success Username="user@example.com"
```

### Error Pattern (Pot Not Available)

```
Operation=OpenPot Status=Started Username="user@example.com"
Operation=OpenPot Status=Failure Username="user@example.com" Exception="InvalidOperationException: The Lucky Pot cannot be opened..."
Operation=OpenPot Status=Success Username="user@example.com"
```

### Fatal Pattern (Crash)

```
Operation=StartUp Status=Success
Fatal Operation=Unknown Status=Failure Exception="System.IO.FileNotFoundException: Could not load file or assembly..."
```

## When to Report Issues

Report if:
- Selectors need updating (UI changed)
- New exception type not handled
- Performance degradation
- Security concern

Include:
- Log file excerpt
- `appsettings.json` (redacted)
- OS / .NET version / Chrome version
- Steps to reproduce

## Related Documentation

- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Configuration](./configuration.md)
- [Build and Deployment](./build-and-deployment.md)
- [FAQ](./faq.md)