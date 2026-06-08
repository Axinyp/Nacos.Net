# Nacos.NET

> .NET client library for [Alibaba Nacos](https://nacos.io) — configuration center, service discovery, gRPC v2 protocol, hot-reload, and AES config encryption. Targets .NET 10.

[![NuGet](https://img.shields.io/nuget/v/Nacos.NET?label=Nacos.NET)](https://www.nuget.org/packages/Nacos.NET)
[![NuGet](https://img.shields.io/nuget/v/Nacos.NET.AspNetCore?label=Nacos.NET.AspNetCore)](https://www.nuget.org/packages/Nacos.NET.AspNetCore)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com)
[![GitHub](https://img.shields.io/badge/GitHub-Axinyp%2FNacos.Net-181717?logo=github)](https://github.com/Axinyp/Nacos.Net)

---

## Packages

| Package | Description |
|---------|-------------|
| [`Nacos.NET`](src/Nacos.NET) | Core SDK — config service, naming service, gRPC v2 transport |
| [`Nacos.NET.Extensions.Configuration`](src/Nacos.NET.Extensions.Configuration) | `IConfiguration` provider with hot-reload |
| [`Nacos.NET.AspNetCore`](src/Nacos.NET.AspNetCore) | ASP.NET Core integration — one-line registration + service instance lifecycle |
| [`Nacos.NET.Config.Encryption`](src/Nacos.NET.Config.Encryption) | Optional AES-256 config decryption filter |

**Dependency graph:**

```
Nacos.NET  (base)
    ├── Nacos.NET.Extensions.Configuration
    ├── Nacos.NET.Config.Encryption
    └── Nacos.NET.AspNetCore  (depends on both above)
```

---

## Quick Start

**ASP.NET Core — full setup in one line:**

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.AddNacos("NacosConfig");  // config center + service registration
```

**`appsettings.json` / `appsettings.Docker.json`:**

```json
{
  "NacosConfig": {
    "ServerAddresses": ["http://nacos-host:8848/"],
    "UserName": "nacos",
    "Password": "nacos",
    "Namespace": "prod",
    "ServiceName": "my-service",
    "GroupName": "DEFAULT_GROUP",
    "Ip": "my-service",
    "Port": "8080",
    "Listeners": [
      {
        "DataId": "appsettings.json",
        "Group": "DEFAULT_GROUP",
        "Optional": false
      }
    ]
  }
}
```

Once registered, config changes in the Nacos console are hot-reloaded into `IConfiguration` — no restart required.

---

## Package Usage

### Nacos.NET — Core SDK only

Use when you need config or naming in non-ASP.NET environments (Worker Service, Console, etc.).

```csharp
// Config service only
services.AddNacosV2Config(configuration, sectionName: "NacosConfig");

// Naming / service discovery only
services.AddNacosV2Naming(configuration, sectionName: "NacosConfig");
```

**Key interfaces:**

| Interface | Responsibility |
|-----------|----------------|
| `INacosConfigService` | Get / publish / listen to config; supports Nacos 3.x FuzzyWatch |
| `INacosNamingService` | Register / deregister instances, service discovery, health subscription |
| `INacosOpenApi` | Nacos management API (namespaces, metrics) |

---

### Nacos.NET.Extensions.Configuration — IConfiguration provider

```csharp
// Recommended: inject at Host build time (before DI container is created)
builder.Host.UseNacosConfig("NacosConfig");

// Or manually:
builder.Host.ConfigureAppConfiguration((_, cfb) => {
    cfb.AddNacosV2Configuration(cfb.Build().GetSection("NacosConfig"));
});
```

Config changes in Nacos are propagated to `IOptionsMonitor<T>` and `IConfiguration` automatically.

---

### Nacos.NET.AspNetCore — full ASP.NET Core integration

```csharp
// One-liner (recommended)
builder.AddNacos("NacosConfig");

// Equivalent manual setup:
builder.Host.UseNacosConfig("NacosConfig");                           // config center
builder.Services.AddNacosAspNet(builder.Configuration, "NacosConfig"); // service registration
```

The service instance is registered with Nacos on app start and deregistered on graceful shutdown.

---

### Nacos.NET.Config.Encryption — AES config decryption

No code changes needed. Declare it in config:

```json
"NacosConfig": {
  "ConfigFilterAssemblies": ["Nacos.NET.Config.Encryption"],
  "ConfigFilterExtInfo": "<Base64-encoded 32-byte AES key>"
}
```

Values stored in Nacos in `ENC(<Base64(IV[16 bytes] + ciphertext)>)` format are transparently decrypted before being fed into `IConfiguration`.

**Offline encryption tool** (to generate values for the Nacos console):

```csharp
string cipher = AesConfigFilter.Encrypt("plain-value", base64Key);
// Paste the ENC(...) string into the Nacos console
```

---

## Choose the right package

| Scenario | Packages |
|----------|----------|
| Config center only (Console / Worker) | `Nacos.NET` + `Nacos.NET.Extensions.Configuration` |
| Service discovery only | `Nacos.NET` |
| ASP.NET Core (config + service registration) | `Nacos.NET.AspNetCore` *(transitively pulls the rest)* |
| AES-encrypted config values | Add `Nacos.NET.Config.Encryption` to any of the above |

---

## Extension method reference

| Method | Package | Purpose |
|--------|---------|---------|
| `builder.AddNacos(section)` | AspNetCore | Config center + service naming in one call |
| `builder.Host.UseNacosConfig(section)` | Extensions.Configuration | Inject Nacos into `IConfiguration` |
| `services.AddNacosAspNet(config, section)` | AspNetCore | Register service instance with Nacos naming |
| `services.AddNacosV2Config(config)` | Nacos.NET | Inject `INacosConfigService` |
| `services.AddNacosV2Naming(config)` | Nacos.NET | Inject `INacosNamingService` |
| `services.AddNacosOpenApi(config)` | Nacos.NET | Inject `INacosOpenApi` (admin use) |

---

## Requirements

- .NET 10.0+
- Nacos 2.x or 3.x server (gRPC v2 protocol)

---

## Acknowledgements

This project is heavily inspired by and partially derived from
**[nacos-sdk-csharp](https://github.com/nacos-group/nacos-sdk-csharp)**,
the official Nacos .NET SDK maintained by the nacos-group community.

Key aspects adopted or referenced from nacos-sdk-csharp:

- gRPC v2 transport layer structure and proto definitions
- `INacosConfigService` / `INacosNamingService` interface contracts
- `IConfigFilter` / `ConfigFilterChainManager` filter chain pattern
- HTTP API client design for Nacos Open API

This library re-implements and extends those concepts targeting .NET 10,
with additions including:

- Nacos 3.x FuzzyWatch config subscription
- AES-256 transparent config decryption (`Nacos.NET.Config.Encryption`)
- Single-call ASP.NET Core registration (`builder.AddNacos()`)
- Structured logging throughout the filter and transport layers

We are grateful to all contributors of nacos-sdk-csharp for their foundational work.

---

## License

Apache License 2.0 — see [LICENSE](LICENSE) for the full text.

This project is distributed under the same license as
[nacos-sdk-csharp](https://github.com/nacos-group/nacos-sdk-csharp)
(Apache-2.0), in compliance with its license terms.
