# Nacos.NET

Nacos C# SDK 核心库，提供配置中心（`INacosConfigService`）和服务注册/发现（`INacosNamingService`）的基础实现。

---

## 快速开始

### 注册服务

```csharp
// 同时注册配置服务与命名服务
builder.Services.AddNacosV2Config(x =>
{
    x.ServerAddresses = new List<string> { "http://localhost:8848/" };
    x.Namespace = "your-namespace";
    x.UserName = "nacos";
    x.Password = "nacos";
});

builder.Services.AddNacosV2Naming(x =>
{
    x.ServerAddresses = new List<string> { "http://localhost:8848/" };
    x.Namespace = "your-namespace";
    x.UserName = "nacos";
    x.Password = "nacos";
});
```

也可以从 `appsettings.json` 读取配置：

```csharp
builder.Services.AddNacosV2Config(builder.Configuration);
builder.Services.AddNacosV2Naming(builder.Configuration);
```

对应 `appsettings.json`：

```json
{
  "NacosConfig": {
    "ServerAddresses": ["http://localhost:8848/"],
    "Namespace": "your-namespace",
    "UserName": "nacos",
    "Password": "nacos"
  }
}
```

### 注入使用

```csharp
public class MyService
{
    private readonly INacosConfigService _config;
    private readonly INacosNamingService _naming;

    public MyService(INacosConfigService config, INacosNamingService naming)
    {
        _config = config;
        _naming = naming;
    }

    public async Task<string> GetRemoteConfig()
    {
        // 读取配置
        return await _config.GetConfig("dataId", "DEFAULT_GROUP", 3000);
    }

    public async Task<string> GetServiceAddress()
    {
        // 获取服务实例
        var instance = await _naming.SelectOneHealthyInstance("my-service", "DEFAULT_GROUP");
        return $"http://{instance.Ip}:{instance.Port}";
    }
}
```

---

## 配置 Filter（扩展点）

`ConfigFilterChainManager` 支持通过 `ConfigFilterAssemblies` 动态加载 `IConfigFilter` 实现，可在配置读取时对内容进行处理（例如解密）。

### 自定义 Filter

实现 `IConfigFilter` 接口（可选实现 `ILoggerAware` 以接收日志实例）：

```csharp
public class MyConfigFilter : IConfigFilter, ILoggerAware
{
    private ILogger _logger = NullLogger.Instance;

    public void SetLogger(ILogger logger) => _logger = logger;

    public void Init(NacosSdkOptions options)
    {
        // 从 options.ConfigFilterExtInfo 读取 Filter 配置
        _logger.LogInformation("MyConfigFilter initialized.");
    }

    public void DoFilter(IConfigRequest request, IConfigResponse response, IConfigFilterChain chain)
    {
        chain.DoFilter(request, response); // 先执行链中其他 Filter

        var content = (response as ConfigResponse)?.GetContent();
        if (!string.IsNullOrEmpty(content))
        {
            // 对 content 做处理后写回
            (response as ConfigResponse)?.SetContent(content);
        }
    }

    public int GetOrder() => 0;

    public string GetFilterName() => nameof(MyConfigFilter);
}
```

在 `appsettings.json` 中注册程序集名，SDK 会通过反射自动实例化 Filter：

```json
{
  "NacosConfig": {
    "ConfigFilterAssemblies": ["MyAssembly.Name"],
    "ConfigFilterExtInfo": "传给 Filter 的扩展参数（如加密密钥）"
  }
}
```

> 项目已内置 AES 加密 Filter，见 `Nacos.NET.Config.Encryption`。
