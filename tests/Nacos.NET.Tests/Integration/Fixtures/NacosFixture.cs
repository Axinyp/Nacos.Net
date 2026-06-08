using Microsoft.Extensions.DependencyInjection;
using Nacos.V2.DependencyInjection;

namespace ConferenceAIOServer.Nacos.Tests.Integration.Fixtures;

/// <summary>
/// 集成测试共享 Fixture — 连接测试 Nacos (172.16.50.165:8848, ns=dev)
/// </summary>
public sealed class NacosFixture : IAsyncLifetime
{
    public const string ServerAddress = "http://127.0.0.1:8848/";
    public const string Namespace     = "dev";
    public const string GroupName     = "server2_0";
    public const string UserName      = "nacos";
    public const string Password      = "nacos";

    public IServiceProvider Services { get; private set; } = null!;

    public INacosConfigService  ConfigService  => Services.GetRequiredService<INacosConfigService>();
    public INacosNamingService  NamingService  => Services.GetRequiredService<INacosNamingService>();

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNacosV2Config(cfg =>
        {
            cfg.ServerAddresses = new System.Collections.Generic.List<string> { ServerAddress };
            cfg.Namespace       = Namespace;
            cfg.UserName        = UserName;
            cfg.Password        = Password;
        });

        services.AddNacosV2Naming(cfg =>
        {
            cfg.ServerAddresses = new System.Collections.Generic.List<string> { ServerAddress };
            cfg.Namespace       = Namespace;
            cfg.UserName        = UserName;
            cfg.Password        = Password;
        });

        Services = services.BuildServiceProvider();

        // warm-up: verify connectivity — 403/null for non-existent key is fine
        try { await ConfigService.GetConfig("test-ping", GroupName, 3000); }
        catch { /* ignore — connectivity will be proved by the actual tests */ }
    }

    public Task DisposeAsync()
    {
        if (Services is IDisposable d) d.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition("Nacos")]
public class NacosCollection : ICollectionFixture<NacosFixture> { }
