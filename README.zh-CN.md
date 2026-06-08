# Nacos.NET

> 适用于 [Alibaba Nacos](https://nacos.io) 的 .NET 客户端库 — 配置中心、服务发现、gRPC v2 协议、热更新、AES 配置加密。目标框架：.NET 10。

[![NuGet](https://img.shields.io/nuget/v/Nacos.NET?label=Nacos.NET)](https://www.nuget.org/packages/Nacos.NET)
[![NuGet](https://img.shields.io/nuget/v/Nacos.NET.AspNetCore?label=Nacos.NET.AspNetCore)](https://www.nuget.org/packages/Nacos.NET.AspNetCore)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com)
[![GitHub](https://img.shields.io/badge/GitHub-Axinyp%2FNacos.Net-181717?logo=github)](https://github.com/Axinyp/Nacos.Net)

[English](README.md) | 中文

---

## 包列表

| 包 | 说明 |
|----|------|
| [`Nacos.NET`](src/Nacos.NET) | 核心 SDK — 配置服务、命名服务、gRPC v2 传输层 |
| [`Nacos.NET.Extensions.Configuration`](src/Nacos.NET.Extensions.Configuration) | `IConfiguration` 提供程序，支持热更新 |
| [`Nacos.NET.AspNetCore`](src/Nacos.NET.AspNetCore) | ASP.NET Core 集成 — 一行注册配置中心和服务注册 |
| [`Nacos.NET.Config.Encryption`](src/Nacos.NET.Config.Encryption) | 可选的 AES-256 配置透明解密 Filter |

**依赖关系：**

```
Nacos.NET（基础包）
    ├── Nacos.NET.Extensions.Configuration
    ├── Nacos.NET.Config.Encryption
    └── Nacos.NET.AspNetCore（依赖以上两个包）
```

---

## 快速开始

**ASP.NET Core — 一行完成全部配置：**

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.AddNacos("NacosConfig");  // 配置中心 + 服务注册
```

**`appsettings.json` / `appsettings.Docker.json`：**

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

注册完成后，在 Nacos 控制台修改配置会自动热更新到 `IConfiguration`，无需重启服务。

---

## 各包使用说明

### Nacos.NET — 核心 SDK

适用于非 ASP.NET Core 环境（Worker Service、Console 应用等）。

```csharp
// 仅注册配置服务
services.AddNacosV2Config(configuration, sectionName: "NacosConfig");

// 仅注册命名/服务发现
services.AddNacosV2Naming(configuration, sectionName: "NacosConfig");
```

**核心接口：**

| 接口 | 职责 |
|------|------|
| `INacosConfigService` | 获取、发布、监听配置；支持 Nacos 3.x FuzzyWatch |
| `INacosNamingService` | 注册/注销实例，服务发现，健康订阅 |
| `INacosOpenApi` | Nacos 管理 API（命名空间、指标） |

---

### Nacos.NET.Extensions.Configuration — IConfiguration 提供程序

```csharp
// 推荐：在 Host 构建阶段注入（DI 容器创建前）
builder.Host.UseNacosConfig("NacosConfig");

// 或手动配置：
builder.Host.ConfigureAppConfiguration((_, cfb) => {
    cfb.AddNacosV2Configuration(cfb.Build().GetSection("NacosConfig"));
});
```

Nacos 配置变更会自动同步到 `IOptionsMonitor<T>` 和 `IConfiguration`。

---

### Nacos.NET.AspNetCore — ASP.NET Core 全功能集成

```csharp
// 一行注册（推荐）
builder.AddNacos("NacosConfig");

// 等价的手动配置：
builder.Host.UseNacosConfig("NacosConfig");                            // 配置中心
builder.Services.AddNacosAspNet(builder.Configuration, "NacosConfig"); // 服务注册
```

服务启动时自动向 Nacos 注册实例，优雅关闭时自动注销。

---

### Nacos.NET.Config.Encryption — AES 配置解密

无需修改代码，在配置文件中声明即可：

```json
"NacosConfig": {
  "ConfigFilterAssemblies": ["Nacos.NET.Config.Encryption"],
  "ConfigFilterExtInfo": "<Base64 编码的 32 字节 AES 密钥>"
}
```

Nacos 控制台中以 `ENC(<Base64(IV[16字节] + 密文)>)` 格式存储的值，会在读取时自动解密后注入 `IConfiguration`。

**离线加密工具**（生成写入 Nacos 控制台的密文）：

```csharp
string cipher = AesConfigFilter.Encrypt("明文内容", base64Key);
// 将 ENC(...) 字符串粘贴到 Nacos 控制台
```

---

## 包选择指南

| 使用场景 | 推荐包 |
|---------|--------|
| 仅配置中心（Console / Worker） | `Nacos.NET` + `Nacos.NET.Extensions.Configuration` |
| 仅服务发现 | `Nacos.NET` |
| ASP.NET Core（配置 + 服务注册） | `Nacos.NET.AspNetCore`（会自动引入其他依赖） |
| AES 加密配置值 | 在上述任意组合基础上添加 `Nacos.NET.Config.Encryption` |

---

## 扩展方法参考

| 方法 | 所在包 | 用途 |
|------|--------|------|
| `builder.AddNacos(section)` | AspNetCore | 一行注册配置中心 + 服务命名 |
| `builder.Host.UseNacosConfig(section)` | Extensions.Configuration | 将 Nacos 注入 `IConfiguration` |
| `services.AddNacosAspNet(config, section)` | AspNetCore | 向 Nacos 命名服务注册实例 |
| `services.AddNacosV2Config(config)` | Nacos.NET | 注入 `INacosConfigService` |
| `services.AddNacosV2Naming(config)` | Nacos.NET | 注入 `INacosNamingService` |
| `services.AddNacosOpenApi(config)` | Nacos.NET | 注入 `INacosOpenApi`（管理用途） |

---

## 环境要求

- .NET 10.0+
- Nacos 2.x 或 3.x 服务端（gRPC v2 协议）

---

## 致谢

本项目大量参考并部分派生自 **[nacos-sdk-csharp](https://github.com/nacos-group/nacos-sdk-csharp)**（Nacos 官方 .NET SDK）。

主要参考内容：

- gRPC v2 传输层结构与 proto 定义
- `INacosConfigService` / `INacosNamingService` 接口契约
- `IConfigFilter` / `ConfigFilterChainManager` 过滤链模式
- Nacos Open API HTTP 客户端设计

在此基础上，本库面向 .NET 10 重新实现并扩展了以下能力：

- Nacos 3.x FuzzyWatch 配置订阅
- AES-256 透明配置解密（`Nacos.NET.Config.Encryption`）
- 单行 ASP.NET Core 注册（`builder.AddNacos()`）
- 过滤层和传输层的结构化日志

感谢 nacos-sdk-csharp 的所有贡献者。

---

## 开源协议

Apache License 2.0 — 详见 [LICENSE](LICENSE)。

本项目与 [nacos-sdk-csharp](https://github.com/nacos-group/nacos-sdk-csharp) 采用相同协议（Apache-2.0）发布。
