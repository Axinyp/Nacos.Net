using Nacos.V2.Config;
using Nacos.V2.Config.Impl;
using Nacos.V2.Remote.Requests;
using Nacos.V2.Remote.Responses;

namespace ConferenceAIOServer.Nacos.Tests.Unit;

/// <summary>
/// ConfigFuzzyWatchNotifyRequestHandler 单元测试
/// 重点验证 C-1（按 group+pattern 路由）和相关边界行为
/// </summary>
public class ConfigFuzzyWatchHandlerTests
{
    // ── 类型不匹配返回 null ───────────────────────────────────────────────────

    [Fact]
    public void RequestReply_Returns_Null_For_Unknown_Request()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        Assert.Null(handler.RequestReply(new HealthCheckRequest()));
    }

    // ── 精确路由：只有订阅对应 (group, pattern) 的 watcher 收到通知 ────────────

    [Fact]
    public void Watcher_Only_Notified_For_Matching_Pattern()
    {
        var handler  = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var yamlW    = new RecordingConfigWatcher();
        var jsonW    = new RecordingConfigWatcher();

        handler.AddWatcher("DEFAULT_GROUP", "*.yaml", yamlW);
        handler.AddWatcher("DEFAULT_GROUP", "*.json", jsonW);

        handler.RequestReply(MakeNotify("DEFAULT_GROUP", "*.json", "content"));

        Assert.Empty(yamlW.Events);           // yaml watcher 不应收到 json 通知
        Assert.Single(jsonW.Events);
    }

    [Fact]
    public void Watcher_Only_Notified_For_Matching_Group()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var groupA  = new RecordingConfigWatcher();
        var groupB  = new RecordingConfigWatcher();

        handler.AddWatcher("GROUP_A", "app.*", groupA);
        handler.AddWatcher("GROUP_B", "app.*", groupB);

        handler.RequestReply(MakeNotify("GROUP_A", "app.*", "v1"));

        Assert.Single(groupA.Events);
        Assert.Empty(groupB.Events);
    }

    // ── 同 pattern 多 watcher：全部收到通知 ─────────────────────────────────

    [Fact]
    public void Multiple_Watchers_On_Same_Pattern_All_Notified()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var w1 = new RecordingConfigWatcher();
        var w2 = new RecordingConfigWatcher();

        handler.AddWatcher("GRP", "svc.*", w1);
        handler.AddWatcher("GRP", "svc.*", w2);

        handler.RequestReply(MakeNotify("GRP", "svc.*", "data"));

        Assert.Single(w1.Events);
        Assert.Single(w2.Events);
    }

    // ── 同一 watcher 订阅多个 pattern：每个 pattern 独立路由 ─────────────────

    [Fact]
    public void Same_Watcher_Multiple_Patterns_Receives_Each_Independently()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var watcher = new RecordingConfigWatcher();

        handler.AddWatcher("GRP", "*.yaml", watcher);
        handler.AddWatcher("GRP", "*.json", watcher);

        handler.RequestReply(MakeNotify("GRP", "*.yaml", "c1"));
        handler.RequestReply(MakeNotify("GRP", "*.json", "c2"));

        Assert.Equal(2, watcher.Events.Count);
    }

    // ── RemoveWatcher 只移除特定 pattern，不影响其他 ────────────────────────

    [Fact]
    public void RemoveWatcher_For_One_Pattern_Does_Not_Affect_Other()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var watcher = new RecordingConfigWatcher();

        handler.AddWatcher("GRP", "*.yaml", watcher);
        handler.AddWatcher("GRP", "*.json", watcher);
        handler.RemoveWatcher("GRP", "*.yaml", watcher);  // 只移除 yaml 订阅

        handler.RequestReply(MakeNotify("GRP", "*.yaml", "y"));
        handler.RequestReply(MakeNotify("GRP", "*.json", "j"));

        Assert.Single(watcher.Events);
        Assert.Equal("*.json", watcher.Events[0].DataId);
    }

    // ── RemoveWatcher 后不再收到该 pattern 的通知 ───────────────────────────

    [Fact]
    public void RemoveWatcher_Stops_Notifications()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var watcher = new RecordingConfigWatcher();

        handler.AddWatcher("GRP", "cfg.*", watcher);
        handler.RemoveWatcher("GRP", "cfg.*", watcher);

        handler.RequestReply(MakeNotify("GRP", "cfg.*", "x"));

        Assert.Empty(watcher.Events);
    }

    // ── GetWatchedPatterns 返回所有已订阅 (group, pattern) 对 ──────────────

    [Fact]
    public void GetWatchedPatterns_Returns_All_Active_Subscriptions()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var w = new RecordingConfigWatcher();

        handler.AddWatcher("GRP_A", "*.yaml", w);
        handler.AddWatcher("GRP_B", "*.json", w);

        var patterns = handler.GetWatchedPatterns().ToList();

        Assert.Equal(2, patterns.Count);
        Assert.Contains(("GRP_A", "*.yaml"), patterns);
        Assert.Contains(("GRP_B", "*.json"), patterns);
    }

    [Fact]
    public void GetWatchedPatterns_Empty_After_All_Removed()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var w = new RecordingConfigWatcher();

        handler.AddWatcher("GRP", "svc.*", w);
        handler.RemoveWatcher("GRP", "svc.*", w);

        Assert.Empty(handler.GetWatchedPatterns());
    }

    // ── watcher 抛异常不影响其他 watcher ──────────────────────────────────

    [Fact]
    public void Throwing_Watcher_Does_Not_Prevent_Others()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var bad     = new ThrowingConfigWatcher();
        var good    = new RecordingConfigWatcher();

        handler.AddWatcher("GRP", "cfg.*", bad);
        handler.AddWatcher("GRP", "cfg.*", good);

        var ex = Record.Exception(() => handler.RequestReply(MakeNotify("GRP", "cfg.*", "v")));

        Assert.Null(ex);
        Assert.Single(good.Events);
    }

    // ── RequestReply 正确映射所有字段 ────────────────────────────────────────

    [Fact]
    public void RequestReply_Maps_All_Fields_Correctly()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var watcher = new RecordingConfigWatcher();

        handler.AddWatcher("GRP", "app.*", watcher);

        handler.RequestReply(new ConfigFuzzyWatchNotifyRequest
        {
            Namespace = "prod",
            Group     = "GRP",
            DataId    = "app.*",
            Content   = "key=value",
            SyncType  = "CHANGE_CONFIG"
        });

        var evt = watcher.Events[0];
        Assert.Equal("prod",          evt.Namespace);
        Assert.Equal("GRP",           evt.Group);
        Assert.Equal("app.*",         evt.DataId);
        Assert.Equal("key=value",     evt.Content);
        Assert.Equal("CHANGE_CONFIG", evt.SyncType);
    }

    // ── RequestReply 返回正确响应类型 ────────────────────────────────────────

    [Fact]
    public void RequestReply_Returns_ConfigFuzzyWatchNotifyResponse()
    {
        var handler = new ConfigFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var resp = handler.RequestReply(MakeNotify("GRP", "p.*", "c"));
        Assert.IsType<ConfigFuzzyWatchNotifyResponse>(resp);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static ConfigFuzzyWatchNotifyRequest MakeNotify(string group, string dataId, string content)
        => new ConfigFuzzyWatchNotifyRequest
        {
            Namespace = "test-ns",
            Group     = group,
            DataId    = dataId,
            Content   = content,
            SyncType  = "CHANGE_CONFIG"
        };

    private sealed class RecordingConfigWatcher : IConfigFuzzyWatcher
    {
        public List<ConfigFuzzyWatchChangeEvent> Events { get; } = new();
        public void OnChange(ConfigFuzzyWatchChangeEvent e) => Events.Add(e);
    }

    private sealed class ThrowingConfigWatcher : IConfigFuzzyWatcher
    {
        public void OnChange(ConfigFuzzyWatchChangeEvent e)
            => throw new InvalidOperationException("simulated failure");
    }
}
