using System.Security.Cryptography;
using Nacos.Config.Encryption;
using Nacos.V2;
using Nacos.V2.Config.FilterImpl;

namespace Nacos.NET.Tests.Unit;

public class AesConfigFilterTests
{
    private static string GenerateBase64Key(int bytes = 32)
    {
        var key = new byte[bytes];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }

    [Fact]
    public void DoFilter_PlainContent_NotModified()
    {
        var filter = CreateFilter(GenerateBase64Key());
        var response = new ConfigResponse();
        response.SetContent("plain-text-value");

        filter.DoFilter(new ConfigRequest(), response, new NoopFilterChain());

        Assert.Equal("plain-text-value", response.GetContent());
    }

    [Fact]
    public void DoFilter_EncryptedContent_Decrypted()
    {
        var base64Key = GenerateBase64Key();
        var cipher = AesConfigFilter.Encrypt("my-secret-password", base64Key);

        var filter = CreateFilter(base64Key);
        var response = new ConfigResponse();
        response.SetContent(cipher);

        filter.DoFilter(new ConfigRequest(), response, new NoopFilterChain());

        Assert.Equal("my-secret-password", response.GetContent());
    }

    [Fact]
    public void Encrypt_ProducesENCFormat()
    {
        var key = GenerateBase64Key();
        var result = AesConfigFilter.Encrypt("value", key);
        Assert.StartsWith("ENC(", result);
        Assert.EndsWith(")", result);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip()
    {
        var key = GenerateBase64Key();
        var original = "jdbc:mysql://localhost:3306/db?password=abc123!@#";
        var cipher = AesConfigFilter.Encrypt(original, key);

        var filter = CreateFilter(key);
        var response = new ConfigResponse();
        response.SetContent(cipher);
        filter.DoFilter(new ConfigRequest(), response, new NoopFilterChain());

        Assert.Equal(original, response.GetContent());
    }

    [Fact]
    public void Init_InvalidKeyLength_Throws()
    {
        var badKey = Convert.ToBase64String(new byte[10]);
        var filter = new AesConfigFilter();
        var options = new NacosSdkOptions { ConfigFilterExtInfo = badKey };
        Assert.Throws<InvalidOperationException>(() => filter.Init(options));
    }

    [Fact]
    public void DoFilter_NoKey_ContentUnchanged()
    {
        var filter = new AesConfigFilter();
        filter.Init(new NacosSdkOptions());          // no key
        var response = new ConfigResponse();
        response.SetContent("ENC(someciphertext)");

        filter.DoFilter(new ConfigRequest(), response, new NoopFilterChain());

        Assert.Equal("ENC(someciphertext)", response.GetContent());
    }

    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void Init_ValidKeyLengths_DoNotThrow(int bytes)
    {
        var key = GenerateBase64Key(bytes);
        var filter = new AesConfigFilter();
        var exception = Record.Exception(
            () => filter.Init(new NacosSdkOptions { ConfigFilterExtInfo = key }));
        Assert.Null(exception);
    }

    // ConfigFilterExtInfo 是 Nacos SDK 预留的 Filter 扩展字段（源自 Java SDK 同名字段）。
    // 本插件约定将其用于传递 AES Key；此测试确认空值时插件静默跳过，不影响无需加密的环境。
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Init_EmptyExtInfo_SilentlySkips(string? extInfo)
    {
        var filter = new AesConfigFilter();
        var exception = Record.Exception(
            () => filter.Init(new NacosSdkOptions { ConfigFilterExtInfo = extInfo! }));
        Assert.Null(exception);
    }

    [Fact]
    public void Init_InvalidBase64_ThrowsInvalidOperationException()
    {
        var filter = new AesConfigFilter();
        var options = new NacosSdkOptions { ConfigFilterExtInfo = "not-valid-base64!!!" };
        var ex = Assert.Throws<InvalidOperationException>(() => filter.Init(options));
        Assert.Contains("not valid Base64", ex.Message);
    }

    [Theory]
    [InlineData("Hello, 世界！")]
    [InlineData("密码：abc123")]
    [InlineData("emoji: 🔑🔒")]
    [InlineData("日本語テスト")]
    public void EncryptDecrypt_Unicode_RoundTrip(string plainText)
    {
        var key = GenerateBase64Key();
        var cipher = AesConfigFilter.Encrypt(plainText, key);

        var filter = CreateFilter(key);
        var response = new ConfigResponse();
        response.SetContent(cipher);
        filter.DoFilter(new ConfigRequest(), response, new NoopFilterChain());

        Assert.Equal(plainText, response.GetContent());
    }

    [Fact]
    public void EncryptDecrypt_ChineseJson_RoundTrip()
    {
        var key = GenerateBase64Key();
        var json = """{"数据库": "mysql://localhost/会议", "密码": "P@ssw0rd!中文", "备注": "生产环境，勿改"}""";
        var cipher = AesConfigFilter.Encrypt(json, key);

        var filter = CreateFilter(key);
        var response = new ConfigResponse();
        response.SetContent(cipher);
        filter.DoFilter(new ConfigRequest(), response, new NoopFilterChain());

        Assert.Equal(json, response.GetContent());
    }

    private static AesConfigFilter CreateFilter(string base64Key)
    {
        var filter = new AesConfigFilter();
        filter.Init(new NacosSdkOptions { ConfigFilterExtInfo = base64Key });
        return filter;
    }

    private sealed class NoopFilterChain : Nacos.V2.Config.Abst.IConfigFilterChain
    {
        public void DoFilter(Nacos.V2.Config.Abst.IConfigRequest request,
                             Nacos.V2.Config.Abst.IConfigResponse response) { }
    }
}
