# Privacy and Personal Data

This document describes how Honeygain Pot Opener at https://github.com/hmlendea/honeygain-pot-opener handles personal data. It covers the application behaviour and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

**Information reviewed:** 2026-10-14

## 📑 Table of Contents

- [What This Document Covers](#what-this-document-covers)
- [Self-Hosted Deployments](#self-hosted-deployments)
- [Data We Handle](#data-we-handle)
  - [Data Provided to the Application](#data-provided-to-the-application)
  - [Data Generated or Collected by the Application](#data-generated-or-collected-by-the-application)
  - [Data Received from Integrations](#data-received-from-integrations)
- [Processing and Use](#processing-and-use)
- [Storage, Retention, and Deletion](#storage-retention-and-deletion)
- [External Processing and Integrations](#external-processing-and-integrations)
- [User Controls and Requests](#user-controls-and-requests)
- [International Transfers](#international-transfers)
- [Children](#children)
- [Data Protection and Security](#data-protection-and-security)
- [Document Changes](#document-changes)
- [Contact](#contact)

## 🔎 What This Document Covers

This document describes how Honeygain Pot Opener at https://github.com/hmlendea/honeygain-pot-opener handles personal data. It covers the application behaviour and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

Honeygain Pot Opener is a self-hosted console application. The instance operator downloads, configures, and executes the application on their own infrastructure. The operator controls the instance's configuration, local storage, logs, backups, access controls, retention, and request handling. The project maintainers do not operate a hosted service and do not receive data from self-hosted instances.

The application does not send telemetry, update checks, crash reports, or any other data to project maintainers or external services beyond the Honeygain dashboard interaction described in this document.

## 📥 Data We Handle

### Data Provided to the Application

- Honeygain account credentials (e-mail address and password) provided by the operator in the local `appsettings.json` configuration file.

### Data Generated or Collected by the Application

- Honeygain e-mail address recorded in structured operation logs (NuciLog) when file output is enabled.

### Data Received from Integrations

- No personal data is received from integrations or third parties beyond the Honeygain dashboard responses during normal operation.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- Honeygain authentication — Honeygain account credentials (e-mail address and password)
- Achievement reward claim — Honeygain account credentials (e-mail address)
- Lucky Pot opening — Honeygain account credentials (e-mail address)
- Operation logging — Honeygain e-mail address (in log records)

## 🗄️ Storage, Retention, and Deletion

| Data Category | Storage Location | Retention | Deletion Control |
|---------------|------------------|-----------|------------------|
| Honeygain credentials | Local `appsettings.json` in application working directory | Retained until operator modifies or deletes the file | Operator controls file contents and deletion |
| Honeygain e-mail address in logs | Configured NuciLog destinations, default `./logfile.log` in application working directory | No automatic retention policy defined | Operator controls log file retention and can disable file output via `nuciLoggerSettings.isFileOutputEnabled` |

The project maintainers do not store, retain, or delete any operator data. All storage is local to the operator's environment.

## 🔗 External Processing and Integrations

| Service or integration | Purpose | Data involved | Configuration or documentation |
|-----------------------|---------|---------------|--------------------------------|
| Honeygain dashboard | Authenticates, claims achievement reward, opens Lucky Pot | Honeygain account credentials (e-mail address and password) transmitted during login; e-mail address in subsequent requests | https://dashboard.honeygain.com |

The application has no other built-in external data transfers.

## ⚙️ User Controls and Requests

The following documented controls are available to the instance operator:
- Modify or delete credentials in `appsettings.json` — Edit the configuration file directly.
- Disable file-based logging — Set `nuciLoggerSettings.isFileOutputEnabled` to `false` in `appsettings.json`.
- Change log file path — Set `nuciLoggerSettings.logFilePath` in `appsettings.json`.
- Change minimum log level — Set `nuciLoggerSettings.minimumLevel` in `appsettings.json`.

## 🌍 International Transfers

The application transmits Honeygain credentials and e-mail address to the Honeygain dashboard over HTTPS. The geographic location of Honeygain's infrastructure is controlled by Honeygain and not by this application. The instance operator controls the deployment location of the application itself.

## 🧒 Children

Honeygain Pot Opener is not directed to children. The application requires a Honeygain account, which has its own age restrictions enforced by Honeygain.

## 🛡️ Data Protection and Security

- Credentials are stored in plain text in the local `appsettings.json` file. The operator is responsible for restricting file permissions and never committing genuine credentials to version control.
- The Honeygain e-mail address appears in operation logs when file output is enabled. The operator can disable file output or restrict log file permissions.
- No secrets, access tokens, or passwords are logged.
- The operator is responsible for keeping the application, .NET runtime, browser, and WebDriver updated.
- The operator controls network exposure, access controls, and backup policies for their deployment.
- The application does not implement encryption at rest for configuration or logs.

## 🔄 Document Changes

Update this document when application data flows, storage, integrations, or deployment responsibilities change. The current version is published at https://github.com/hmlendea/honeygain-pot-opener/blob/master/PRIVACY.md.

## 📬 Contact

For questions about application data handling, contact the project maintainers through GitHub. For a self-hosted instance, the instance operator is the data controller. Include the repository name and deployment context; do not send passwords, access tokens, or other secrets.