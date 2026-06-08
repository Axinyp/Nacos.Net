using Nacos.V2.Naming.Remote.Grpc;
using Nacos.V2.Naming;
using Nacos.V2.Naming.Dtos;
using Nacos.V2.Remote.Requests;

namespace ConferenceAIOServer.Nacos.Tests.Unit;

/// <summary>
/// NamingFuzzyWatchNotifyRequestHandler 单元测试
/// </summary>
public class FuzzyWatchHandlerTests
{
    // ── RequestReply 返回 null 当请求类型不匹配 ───────────────────────────────

    [Fact]
    public void RequestReply_Returns_Null_For_Unknown_Request()
    {
        var handler = new NamingFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var result = handler.RequestReply(new HealthCheckRequest());
        Assert.Null(result);
    }

    // ── AddWatcher + 通知分发 ─────────────────────────────────────────────────

    [Fact]
    public void AddWatcher_Receives_Notification()
    {
        var handler = new NamingFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var watcher = new RecordingFuzzyWatcher();
        handler.AddWatcher(watcher);

        var notifyReq = new NamingFuzzyWatchNotifyRequest
        {
            Namespace   = "dev",
            GroupName   = "DEFAULT_GROUP",
            ServiceName = "my-service",
            SyncType    = "ADD"
        };

        var response = handler.RequestReply(notifyReq);

        Assert.NotNull(response);
        Assert.IsType<global::Nacos.V2.Remote.Responses.NamingFuzzyWatchNotifyResponse>(response);
        Assert.Single(watcher.ReceivedEvents);
        Assert.Equal("my-service", watcher.ReceivedEvents[0].ServiceName);
        Assert.Equal("ADD",        watcher.ReceivedEvents[0].SyncType);
    }

    // ── 多个 watcher 都收到通知 ─────────────────────────────────────────────

    [Fact]
    public void Multiple_Watchers_All_Receive_Notification()
    {
        var handler  = new NamingFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var watcher1 = new RecordingFuzzyWatcher();
        var watcher2 = new RecordingFuzzyWatcher();
        handler.AddWatcher(watcher1);
        handler.AddWatcher(watcher2);

        handler.RequestReply(new NamingFuzzyWatchNotifyRequest { Namespace = "ns", SyncType = "DELETE" });

        Assert.Single(watcher1.ReceivedEvents);
        Assert.Single(watcher2.ReceivedEvents);
    }

    // ── RemoveWatcher 后不再收到通知 ──────────────────────────────────────────

    [Fact]
    public void RemoveWatcher_Stops_Receiving_Notifications()
    {
        var handler = new NamingFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var watcher = new RecordingFuzzyWatcher();
        handler.AddWatcher(watcher);
        handler.RemoveWatcher(watcher);

        handler.RequestReply(new NamingFuzzyWatchNotifyRequest { Namespace = "ns", SyncType = "ADD" });

        Assert.Empty(watcher.ReceivedEvents);
    }

    // ── watcher 抛异常不影响其他 watcher ──────────────────────────────────────

    [Fact]
    public void Throwing_Watcher_Does_Not_Prevent_Others_From_Being_Notified()
    {
        var handler   = new NamingFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var bad       = new ThrowingFuzzyWatcher();
        var good      = new RecordingFuzzyWatcher();
        handler.AddWatcher(bad);
        handler.AddWatcher(good);

        var ex = Record.Exception(() =>
            handler.RequestReply(new NamingFuzzyWatchNotifyRequest { SyncType = "ADD" }));

        Assert.Null(ex);
        Assert.Single(good.ReceivedEvents);
    }

    // ── event 字段映射正确 ────────────────────────────────────────────────────

    [Fact]
    public void RequestReply_Maps_All_Fields_Correctly()
    {
        var handler = new NamingFuzzyWatchNotifyRequestHandler(NullLogger.Instance);
        var watcher = new RecordingFuzzyWatcher();
        handler.AddWatcher(watcher);

        var svcInfo = new ServiceInfo();
        handler.RequestReply(new NamingFuzzyWatchNotifyRequest
        {
            Namespace   = "prod",
            GroupName   = "grp",
            ServiceName = "svc",
            ServiceInfo = svcInfo,
            SyncType    = "CHANGED"
        });

        var evt = watcher.ReceivedEvents[0];
        Assert.Equal("prod",    evt.Namespace);
        Assert.Equal("grp",     evt.GroupName);
        Assert.Equal("svc",     evt.ServiceName);
        Assert.Same(svcInfo,    evt.ServiceInfo);
        Assert.Equal("CHANGED", evt.SyncType);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private sealed class RecordingFuzzyWatcher : IFuzzyWatcher
    {
        public List<FuzzyWatchChangeEvent> ReceivedEvents { get; } = new();
        public void OnChange(FuzzyWatchChangeEvent changeEvent) => ReceivedEvents.Add(changeEvent);
    }

    private sealed class ThrowingFuzzyWatcher : IFuzzyWatcher
    {
        public void OnChange(FuzzyWatchChangeEvent changeEvent)
            => throw new InvalidOperationException("simulated failure");
    }
}
