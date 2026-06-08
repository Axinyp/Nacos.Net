using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nacos.V2;
using Nacos.V2.Config.Abst;
using Nacos.V2.Config.FilterImpl;

namespace Nacos.Config.Encryption;

/// <summary>
/// Nacos 配置 AES-CBC 客户端解密 Filter。
/// Nacos 控制台存密文（ENC(base64ciphertext)），SDK 读取后在本地自动解密为明文，服务端无感知。
///
/// <para><b>接入方式（appsettings.json）</b></para>
/// <code>
/// "NacosConfig": {
///   "ConfigFilterAssemblies": ["Nacos.NET.Config.Encryption"],
///   // ConfigFilterExtInfo 是 Nacos SDK 的通用扩展字段（源自 Java SDK 同名字段），
///   // 在本插件中约定用于传递 AES Key（Base64 编码的 16/24/32 字节密钥）。
///   // 生产环境建议通过环境变量覆盖，避免密钥进入配置文件：
///   //   NacosConfig__ConfigFilterExtInfo=&lt;base64-key&gt;
///   "ConfigFilterExtInfo": ""
/// }
/// </code>
///
/// <para><b>生成密文</b>：调用 <see cref="Encrypt"/> 将明文转为 ENC(xxx) 格式后写入 Nacos 控制台。</para>
/// <para><b>加密算法</b>：AES-CBC，随机 IV 前置于密文（IV 16 字节 + 密文），PKCS7 填充。</para>
/// </summary>
public sealed class AesConfigFilter : IConfigFilter, ILoggerAware
{
    private const string Prefix = "ENC(";
    private const string Suffix = ")";

    private byte[] _key = [];
    private ILogger _logger = NullLogger.Instance;

    public void SetLogger(ILogger logger) => _logger = logger;

    public void Init(NacosSdkOptions options)
    {
        // ConfigFilterExtInfo 是 Nacos SDK 预留的 Filter 扩展字段（Java SDK 同名）。
        // 本插件约定：此字段传入 Base64 编码的 AES 密钥（16/24/32 字节）。
        var extInfo = options.ConfigFilterExtInfo;
        if (string.IsNullOrWhiteSpace(extInfo))
        {
            _logger.LogWarning("[AesConfigFilter] ConfigFilterExtInfo is empty — decryption is DISABLED. " +
                               "Set NacosConfig:ConfigFilterExtInfo to a Base64-encoded 32-byte AES key.");
            return;
        }

        try
        {
            _key = Convert.FromBase64String(extInfo.Trim());
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "[AesConfigFilter] ConfigFilterExtInfo is not valid Base64. " +
                "Ensure the key was generated with Convert.ToBase64String and not modified manually.", ex);
        }

        if (_key.Length is not (16 or 24 or 32))
            throw new InvalidOperationException(
                $"[AesConfigFilter] AES key must be 16, 24, or 32 bytes after Base64 decode; got {_key.Length}.");

        _logger.LogInformation("[AesConfigFilter] Initialized with AES-{Bits} key. " +
                               "ENC(...) config values will be decrypted automatically.",
            _key.Length * 8);
    }

    public void DoFilter(IConfigRequest request, IConfigResponse response, IConfigFilterChain filterChain)
    {
        filterChain.DoFilter(request, response);

        if (_key.Length == 0)
            return;

        var content = (response as ConfigResponse)?.GetContent();
        if (string.IsNullOrEmpty(content))
            return;

        if (content.StartsWith(Prefix, StringComparison.Ordinal)
            && content.EndsWith(Suffix, StringComparison.Ordinal))
        {
            try
            {
                var cipher = content[Prefix.Length..^Suffix.Length];
                var plain = Decrypt(cipher, _key);
                (response as ConfigResponse)?.SetContent(plain);

                _logger.LogDebug("[AesConfigFilter] Decrypted config: {CipherLen} chars → {PlainLen} chars.",
                    content.Length, plain.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AesConfigFilter] Decryption failed. " +
                    "Verify the key in ConfigFilterExtInfo matches the one used for encryption. " +
                    "Cipher prefix: {Prefix}",
                    content.Length > 20 ? content[..20] + "…" : content);
                throw;
            }
        }
        else
        {
            _logger.LogDebug("[AesConfigFilter] Config is plaintext, no decryption needed.");
        }
    }

    public int GetOrder() => 0;

    public string GetFilterName() => nameof(AesConfigFilter);

    // AES-CBC, PKCS7 padding, IV prepended to cipher bytes
    private static string Decrypt(string base64Cipher, byte[] key)
    {
        var data = Convert.FromBase64String(base64Cipher);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = data[..16];
        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(data, 16, data.Length - 16);
        return Encoding.UTF8.GetString(plain);
    }

    /// <summary>
    /// 离线加密工具方法：将明文加密为 ENC(xxx) 格式，用于写入 Nacos 控制台。
    /// </summary>
    public static string Encrypt(string plainText, string base64Key)
    {
        var key = Convert.FromBase64String(base64Key.Trim());
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var combined = new byte[16 + cipher.Length];
        aes.IV.CopyTo(combined, 0);
        cipher.CopyTo(combined, 16);
        return $"{Prefix}{Convert.ToBase64String(combined)}{Suffix}";
    }
}
