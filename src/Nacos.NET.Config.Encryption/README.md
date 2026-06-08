# Nacos.NET.Config.Encryption

为 Nacos.NET 提供 **AES-256 透明解密** 能力。Nacos 控制台中以密文存储的配置值，在应用读取时自动解密为明文，业务代码无需任何改动。

---

## 安装

```
dotnet add package Nacos.NET.Config.Encryption
```

需同时引用 `Nacos.NET.Extensions.Configuration`（或 `Nacos.NET.AspNetCore`）以接入配置中心。

---

## 快速开始

### 1. 启用解密 Filter

在 `appsettings.json` 中声明程序集和密钥：

```json
{
  "NacosConfig": {
    "ServerAddresses": ["http://localhost:8848/"],
    "Namespace": "your-namespace",
    "UserName": "nacos",
    "Password": "nacos",
    "ConfigFilterAssemblies": ["Nacos.NET.Config.Encryption"],
    "ConfigFilterExtInfo": "<Base64 编码的 32 字节 AES 密钥>"
  }
}
```

无需修改任何代码，SDK 通过反射自动加载 Filter。

### 2. 生成密钥

```csharp
using var aes = Aes.Create();
aes.KeySize = 256;
aes.GenerateKey();
string base64Key = Convert.ToBase64String(aes.Key);
Console.WriteLine(base64Key); // 将此值写入 ConfigFilterExtInfo
```

### 3. 加密配置内容

使用内置工具方法生成密文，再粘贴到 Nacos 控制台：

```csharp
string base64Key = "your-base64-key";
string plainJson = File.ReadAllText("appsettings.json");
string encrypted = AesConfigFilter.Encrypt(plainJson, base64Key);
File.WriteAllText("output.txt", encrypted);
// 将 output.txt 中的 ENC(...) 内容完整复制到 Nacos 控制台
```

> 从文件复制密文，不要从终端复制，避免换行截断密文。

---

## 工作原理

| 步骤 | 说明 |
|------|------|
| 存储格式 | `ENC(Base64(IV[16字节] + 密文字节))` |
| 读取时机 | SDK 从 Nacos 拉取配置后、写入 `IConfiguration` 前 |
| 透明处理 | 非 `ENC(...)` 格式的普通值直接透传，不受影响 |

### 加密算法参数

| 项 | 值 |
|---|---|
| 算法 | AES-CBC |
| 填充 | PKCS7 |
| 密钥长度 | 128 / 192 / 256 bit（16 / 24 / 32 字节） |
| IV | 随机生成，每次加密不同，前置于密文 |

---

## 生产环境密钥管理

**不要**将密钥写入 `appsettings.json` 提交到代码仓库，改用环境变量注入：

```
NacosConfig__ConfigFilterExtInfo=<base64-key>
```

Docker Compose 示例：

```yaml
environment:
  - NacosConfig__ConfigFilterExtInfo=${AES_KEY}
```

---

## 常见问题

| 现象 | 原因 | 解决 |
|------|------|------|
| 配置值仍显示 `ENC(...)` | dll 不在输出目录 | 在 `.csproj` 中添加对本包的引用 |
| `Warning: ConfigFilterExtInfo is empty` | 未配置密钥 | 填写 `ConfigFilterExtInfo` 或检查环境变量 |
| `Error: Decryption failed` | 密钥与加密时不一致，或密文被截断 | 重新核对密钥；从文件完整复制密文后重新写入 Nacos |
| `Warning: No IConfigFilter found` | 程序集名拼写错误 | 确认 `ConfigFilterAssemblies` 值为 `Nacos.NET.Config.Encryption` |
