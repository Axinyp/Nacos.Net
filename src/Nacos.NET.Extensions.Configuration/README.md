# Nacos.NET.Extensions.Configuration

将 Nacos 配置中心集成到 ASP.NET Core `IConfiguration` 体系，支持配置热更新与 AES 加密解密。

---

## 快速开始

### 1. 注册到 Host

在 `Program.cs` 中调用 `UseNacosConfig`：

```csharp
builder.Host.UseNacosConfig(section: "NacosConfig");
```

或手动构建：

```csharp
builder.Host.ConfigureAppConfiguration((_, cfb) =>
{
    var config = cfb.Build();
    cfb.AddNacosV2Configuration(config.GetSection("NacosConfig"));
});
```

### 2. `appsettings.json` 配置

```json
{
  "NacosConfig": {
    "ServerAddresses": ["http://localhost:8848/"],
    "Namespace": "your-namespace",
    "GroupName": "DEFAULT_GROUP",
    "UserName": "nacos",
    "Password": "nacos",
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

配置加载成功后，Nacos 中的内容会合并到 `IConfiguration`，可直接通过 `builder.Configuration["Key"]` 或 `IOptions<T>` 读取，并在 Nacos 控制台更新配置后自动热更新。

### 字段说明

| 字段 | 说明 |
|------|------|
| `Listeners[].DataId` | Nacos 控制台中的配置 Data ID |
| `Listeners[].Group` | 配置所属分组，与 `GroupName` 保持一致 |
| `Listeners[].Optional` | `false`：配置不存在时启动报错；`true`：跳过 |
| `Namespace` | 命名空间 ID，空字符串表示 public |
| `GroupName` | 默认分组名 |
| `ConfigFilterAssemblies` | AES 加密 Filter 所在程序集名（不使用加密可省略） |
| `ConfigFilterExtInfo` | 传给 Filter 的扩展参数（AES 场景：Base64 编码的密钥） |

---

## AES 配置加密

需要额外引用 `Nacos.NET.Config.Encryption`，并在 `appsettings.json` 中启用：

```json
{
  "NacosConfig": {
    "ConfigFilterAssemblies": ["Nacos.NET.Config.Encryption"],
    "ConfigFilterExtInfo": "<Base64 编码的 32 字节 AES 密钥>"
  }
}
```

### 原理

- Nacos 控制台中存储的是**密文**，格式：`ENC(<Base64(IV[16字节] + 密文)>)`
- SDK 读取配置后，`AesConfigFilter` 在本地自动解密，应用代码直接读到明文，无需任何改动
- 未使用 `ENC(...)` 格式的普通配置会透明通过，不受影响

### 加密算法

| 项 | 值 |
|---|---|
| 算法 | AES-CBC |
| 填充 | PKCS7 |
| 密钥长度 | 128 / 192 / 256 bit（16 / 24 / 32 字节） |
| IV | 随机生成，每次加密不同，前置于密文 |
| 存储格式 | `ENC(Base64(IV + CipherBytes))` |

### 生成密钥

```csharp
using var aes = Aes.Create();
aes.KeySize = 256;
aes.GenerateKey();
var base64Key = Convert.ToBase64String(aes.Key);
Console.WriteLine(base64Key); // 将此值写入 ConfigFilterExtInfo
```

### 加密配置内容

使用 `AesConfigFilter.Encrypt` 方法将明文配置加密后写入 Nacos 控制台：

```csharp
string base64Key = "your-base64-key";
string plainJson = File.ReadAllText("appsettings.json");
string encrypted = AesConfigFilter.Encrypt(plainJson, base64Key);
File.WriteAllText("output.txt", encrypted);
// 将 output.txt 中的 ENC(...) 内容粘贴到 Nacos 控制台
```

> 从文件复制密文，不要从终端复制，避免终端换行截断密文。

### 生产环境密钥管理

生产环境不应将密钥写入配置文件，通过环境变量注入：

```
NacosConfig__ConfigFilterExtInfo=<base64-key>
```

---

## 调试日志

组件内置结构化日志，启动时自动输出 Filter 加载状态：

| 场景 | 日志级别 | 内容示例 |
|------|---------|---------|
| Filter 成功注册 | `Information` | `[Nacos] Config filter registered: AesConfigFilter (order=0)` |
| 密钥初始化成功 | `Information` | `[AesConfigFilter] Initialized with AES-256 key.` |
| 密钥未配置 | `Warning` | `[AesConfigFilter] ConfigFilterExtInfo is empty — decryption is DISABLED.` |
| 程序集未找到 | `Warning` | `[Nacos] Filter assembly not found: Nacos.NET.Config.Encryption.` |
| 解密成功 | `Debug` | `[AesConfigFilter] Decrypted config: 9969 chars → 6482 chars.` |
| 解密失败 | `Error` | `[AesConfigFilter] Decryption failed. Verify the key...` |

将日志级别设为 `Debug` 可观察每次解密过程：

```json
{
  "Logging": {
    "LogLevel": {
      "Nacos": "Debug"
    }
  }
}
```

---

## 常见问题

| 现象 | 原因 | 解决 |
|------|------|------|
| 配置值仍为 `ENC(...)` | `Nacos.NET.Config.Encryption.dll` 不在输出目录 | 在项目 `.csproj` 中添加对该库的引用 |
| Warning: `ConfigFilterExtInfo is empty` | 未配置 AES 密钥 | 填写 `NacosConfig:ConfigFilterExtInfo` 或检查环境变量 |
| Error: `Decryption failed` | 密钥与加密时不一致，或密文被截断 | 重新核对密钥；从文件复制完整密文后重新写入 Nacos |
| Warning: `No IConfigFilter found` | 程序集名称拼写错误 | 确认 `ConfigFilterAssemblies` 值拼写正确 |
| 配置热更新不生效 | `Listeners` 未配置，或 `DataId` 与 Nacos 控制台不一致 | 检查 `Listeners[].DataId` 是否与 Nacos 中的配置 ID 完全一致 |
