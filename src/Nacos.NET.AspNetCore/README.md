# Nacos.NET.AspNetCore

将 ASP.NET Core 服务实例注册到 Nacos 命名服务，支持服务发现与健康检查。

引用此包后同时获得 `Nacos.NET.Extensions.Configuration` 的能力，可通过一句话同时注册**配置中心**和**服务命名**。

---

## 快速开始（推荐）

### 一键注册：配置中心 + 服务注册

在 `Program.cs` 中调用 `builder.AddNacos()`，一行完成所有配置：

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddNacos(); // 默认读取 appsettings.json 中的 "NacosConfig" 节

var app = builder.Build();
app.Run();
```

等价于手动调用：

```csharp
builder.Host.UseNacosConfig("NacosConfig");          // 配置中心（热更新）
builder.Services.AddNacosAspNet(builder.Configuration, "NacosConfig"); // 服务注册
```

自定义配置节名：

```csharp
builder.AddNacos("MyNacosSection");
```

---

## 仅注册服务命名（不含配置中心）

在 `Program.cs` 中调用 `AddNacosAspNet`：

```csharp
builder.Services.AddNacosAspNet(x =>
{
    x.ServerAddresses = new List<string> { "http://localhost:8848/" };
    x.Namespace = "your-namespace";
    x.GroupName = "DEFAULT_GROUP";
    x.ServiceName = "my-service";
    x.Ip = "192.168.1.100";   // 注册到 Nacos 的 IP（留空则自动获取本机 IP）
    x.Port = 5000;             // 注册到 Nacos 的端口（留空则自动获取监听端口）
    x.Weight = 100;
    x.Ephemeral = true;        // 临时实例（服务下线后自动注销）
});
```

或从 `appsettings.json` 读取：

```csharp
builder.Services.AddNacosAspNet(builder.Configuration, "NacosConfig");
```

### `appsettings.json` 配置

```json
{
  "NacosConfig": {
    "ServerAddresses": ["http://localhost:8848/"],
    "Namespace": "your-namespace",
    "GroupName": "DEFAULT_GROUP",
    "UserName": "nacos",
    "Password": "nacos",
    "ServiceName": "my-service",
    "Ip": "",
    "Port": 0
  }
}
```

> `Ip` 和 `Port` 留空时，SDK 自动获取本机 IP 和应用监听端口。在 **Docker/容器** 环境中，容器内自动获取到的 IP 是容器内网地址，其他服务无法通过该地址访问，因此必须显式配置：
>
> ```json
> {
>   "NacosConfig": {
>     "ServiceName": "my-service",
>     "Ip": "my-service",
>     "Port": "8080"
>   }
> }
> ```
>
> 其中 `Ip` 填写 Docker 网络中该容器可被其他容器访问的主机名（通常是容器名）。

---

## 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| `ServerAddresses` | `List<string>` | Nacos 服务端地址列表 |
| `Namespace` | `string` | 命名空间 ID（空字符串表示 public） |
| `GroupName` | `string` | 服务分组，默认 `DEFAULT_GROUP` |
| `ServiceName` | `string` | 注册的服务名，网关按此名称路由 |
| `Ip` | `string` | 对外可达的 IP 或主机名，容器环境必须填写 |
| `Port` | `int` | 对外可达的端口，容器环境必须填写 |
| `Weight` | `float` | 负载权重，默认 100 |
| `Ephemeral` | `bool` | 是否为临时实例，建议 `true` |
| `RegisterEnabled` | `bool` | 是否启用注册，默认 `true` |

---

## 常见问题

| 现象 | 原因 | 解决 |
|------|------|------|
| 其他服务无法连接到该实例 | `Ip` 注册为容器内网地址，外部不可达 | 在 `appsettings.json` 中显式配置可达的 `Ip` 和 `Port` |
| 实例注册到错误的 Namespace | `Namespace` 配置错误 | 检查与 Nacos 控制台中的命名空间 ID 是否一致 |
| 服务下线后实例仍显示 | `Ephemeral = false` 且未手动注销 | 改为 `Ephemeral = true` 使 Nacos 自动清理 |
| 注册成功但服务发现找不到 | `GroupName` 不一致 | 注册方和查询方的 `GroupName` 必须相同 |
