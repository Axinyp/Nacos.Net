using Nacos.V2.Config.Impl;
using Nacos.V2.Config.FilterImpl;
using System.Collections.Generic;
using NacosSdkOptions = global::Nacos.V2.NacosSdkOptions;

namespace ConferenceAIOServer.Nacos.Tests.Unit;

/// <summary>
/// CacheData.ComputeChanges diff 逻辑单元测试
/// （通过 CheckListenerMd5 + ManagerListenerWrap 间接验证 diff 结果）
/// </summary>
public class CacheDataDiffTests
{
    // ── 辅助：构造一个带监听器的 CacheData，触发通知并捕获事件 ─────────────────

    private static (CacheData cache, RecordingChangeListener listener) BuildCache(
        string initialContent, string type = "text")
    {
        var filterMgr = new ConfigFilterChainManager(new NacosSdkOptions());
        var cache = new CacheData(filterMgr, "test-app", "test-dataId", "DEFAULT_GROUP", "dev");
        cache.SetContent(initialContent ?? string.Empty);
        cache.Type = type;

        var listener = new RecordingChangeListener();
        cache.AddListener(listener);
        return (cache, listener);
    }

    // ── properties diff ──────────────────────────────────────────────────────

    [Fact]
    public void Properties_Added_Key_Is_Detected()
    {
        var (cache, listener) = BuildCache("a=1\nb=2", "properties");
        cache.SetContent("a=1\nb=2\nc=3");
        cache.CheckListenerMd5();

        Assert.NotNull(listener.LastEvent);
        Assert.True(listener.LastEvent!.Changes.ContainsKey("c"));
        Assert.Equal(ConfigChangeType.Added, listener.LastEvent.Changes["c"].Type);
        Assert.Equal("3", listener.LastEvent.Changes["c"].NewValue);
        Assert.Null(listener.LastEvent.Changes["c"].OldValue);
    }

    [Fact]
    public void Properties_Modified_Key_Is_Detected()
    {
        var (cache, listener) = BuildCache("a=1\nb=2", "properties");
        cache.SetContent("a=1\nb=99");
        cache.CheckListenerMd5();

        Assert.NotNull(listener.LastEvent);
        Assert.True(listener.LastEvent!.Changes.ContainsKey("b"));
        Assert.Equal(ConfigChangeType.Modified, listener.LastEvent.Changes["b"].Type);
        Assert.Equal("2", listener.LastEvent.Changes["b"].OldValue);
        Assert.Equal("99", listener.LastEvent.Changes["b"].NewValue);
    }

    [Fact]
    public void Properties_Deleted_Key_Is_Detected()
    {
        var (cache, listener) = BuildCache("a=1\nb=2", "properties");
        cache.SetContent("a=1");
        cache.CheckListenerMd5();

        Assert.NotNull(listener.LastEvent);
        Assert.True(listener.LastEvent!.Changes.ContainsKey("b"));
        Assert.Equal(ConfigChangeType.Deleted, listener.LastEvent.Changes["b"].Type);
        Assert.Equal("2", listener.LastEvent.Changes["b"].OldValue);
        Assert.Null(listener.LastEvent.Changes["b"].NewValue);
    }

    [Fact]
    public void Properties_Unchanged_Produces_No_Event()
    {
        var (cache, listener) = BuildCache("a=1", "properties");
        // same content → md5 matches → no notification
        cache.CheckListenerMd5();

        Assert.Null(listener.LastEvent);
    }

    [Fact]
    public void Properties_Comment_Lines_Are_Ignored()
    {
        var (cache, listener) = BuildCache("# comment\na=1", "properties");
        cache.SetContent("# changed comment\na=1");
        cache.CheckListenerMd5();

        // only the comment changed — no key-level diff
        Assert.Null(listener.LastEvent);
    }

    // ── JSON diff ────────────────────────────────────────────────────────────

    [Fact]
    public void Json_TopLevel_Added_Key_Is_Detected()
    {
        var (cache, listener) = BuildCache("{\"x\":1}", "json");
        cache.SetContent("{\"x\":1,\"y\":2}");
        cache.CheckListenerMd5();

        Assert.NotNull(listener.LastEvent);
        Assert.True(listener.LastEvent!.Changes.ContainsKey("y"));
        Assert.Equal(ConfigChangeType.Added, listener.LastEvent.Changes["y"].Type);
    }

    [Fact]
    public void Json_TopLevel_Modified_Key_Is_Detected()
    {
        var (cache, listener) = BuildCache("{\"x\":1}", "json");
        cache.SetContent("{\"x\":42}");
        cache.CheckListenerMd5();

        Assert.NotNull(listener.LastEvent);
        Assert.Equal(ConfigChangeType.Modified, listener.LastEvent.Changes["x"].Type);
        Assert.Equal("1", listener.LastEvent.Changes["x"].OldValue);
        Assert.Equal("42", listener.LastEvent.Changes["x"].NewValue);
    }

    [Fact]
    public void Json_Invalid_Content_Falls_Back_To_Empty_Map()
    {
        var (cache, listener) = BuildCache("{not-valid}", "json");
        cache.SetContent("{\"x\":1}");
        cache.CheckListenerMd5();

        // old was unparseable → old map empty → all new keys are "Added"
        Assert.NotNull(listener.LastEvent);
        Assert.True(listener.LastEvent!.Changes.ContainsKey("x"));
        Assert.Equal(ConfigChangeType.Added, listener.LastEvent.Changes["x"].Type);
    }

    // ── text diff ────────────────────────────────────────────────────────────

    [Fact]
    public void Text_Modified_Produces_Single_Content_Key()
    {
        var (cache, listener) = BuildCache("hello", "text");
        cache.SetContent("world");
        cache.CheckListenerMd5();

        Assert.NotNull(listener.LastEvent);
        Assert.True(listener.LastEvent!.Changes.ContainsKey("content"));
        Assert.Equal(ConfigChangeType.Modified, listener.LastEvent.Changes["content"].Type);
        Assert.Equal("hello", listener.LastEvent.Changes["content"].OldValue);
        Assert.Equal("world", listener.LastEvent.Changes["content"].NewValue);
    }

    [Fact]
    public void Text_Null_Old_Is_Added()
    {
        var filterMgr = new ConfigFilterChainManager(new NacosSdkOptions());
        // Start with null/empty then set real content
        var cache = new CacheData(filterMgr, "app", "did", "group", "ns");
        cache.Type = "text";
        var listener = new RecordingChangeListener();
        cache.AddListener(listener);

        cache.SetContent("first-value");
        cache.CheckListenerMd5();

        Assert.NotNull(listener.LastEvent);
        Assert.Equal(ConfigChangeType.Added, listener.LastEvent!.Changes["content"].Type);
    }

    // ── listener concurrency ─────────────────────────────────────────────────

    [Fact]
    public async Task AddAndRemoveListener_Thread_Safe()
    {
        var filterMgr = new ConfigFilterChainManager(new NacosSdkOptions());
        var cache = new CacheData(filterMgr, "app", "did", "group", "ns");

        var tasks = new List<System.Threading.Tasks.Task>();
        var listeners = new List<RecordingChangeListener>();

        for (int i = 0; i < 20; i++)
        {
            var l = new RecordingChangeListener();
            listeners.Add(l);
            tasks.Add(System.Threading.Tasks.Task.Run(() => cache.AddListener(l)));
        }

        await System.Threading.Tasks.Task.WhenAll(tasks.ToArray());

        // concurrent removes should not throw
        var removeTasks = new List<System.Threading.Tasks.Task>();
        foreach (var l in listeners)
            removeTasks.Add(System.Threading.Tasks.Task.Run(() => cache.RemoveListener(l)));
        await System.Threading.Tasks.Task.WhenAll(removeTasks.ToArray());

        Assert.Empty(cache.GetListeners());
    }

    // ── IConfigChangeListener is called ──────────────────────────────────────

    [Fact]
    public void IConfigChangeListener_ReceiveConfigChange_Is_Called()
    {
        var (cache, listener) = BuildCache("a=1", "properties");
        cache.SetContent("a=2");
        cache.CheckListenerMd5();

        Assert.NotNull(listener.LastEvent);
        Assert.Single(listener.LastEvent!.Changes);
    }

    [Fact]
    public void Regular_IListener_Still_Gets_ReceiveConfigInfo()
    {
        var filterMgr = new ConfigFilterChainManager(new NacosSdkOptions());
        var cache = new CacheData(filterMgr, "app", "did", "group", "ns");
        cache.SetContent("v1");

        var basic = new BasicListener();
        cache.AddListener(basic);
        cache.SetContent("v2");
        cache.CheckListenerMd5();

        Assert.Equal("v2", basic.LastContent);
    }

    // ── diff baseline uses filtered (post-filter) content ────────────────────

    [Fact]
    public void Diff_Baseline_Uses_Post_Filter_Content_On_Second_Change()
    {
        var (cache, listener) = BuildCache("a=1", "properties");

        // First change: a=1 → a=2
        cache.SetContent("a=2");
        cache.CheckListenerMd5();
        Assert.Equal("2", listener.LastEvent!.Changes["a"].NewValue);

        // Reset event tracking
        listener.LastEvent = null;

        // Second change: a=2 → a=3; OldValue should be "2", not "1"
        cache.SetContent("a=3");
        cache.CheckListenerMd5();

        Assert.NotNull(listener.LastEvent);
        Assert.Equal("2", listener.LastEvent!.Changes["a"].OldValue);
        Assert.Equal("3", listener.LastEvent.Changes["a"].NewValue);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private sealed class RecordingChangeListener : IConfigChangeListener
    {
        public ConfigChangeEvent? LastEvent { get; set; }
        public string? LastContent { get; private set; }

        public void ReceiveConfigInfo(string configInfo) => LastContent = configInfo;

        public void ReceiveConfigChange(ConfigChangeEvent changeEvent) => LastEvent = changeEvent;
    }

    private sealed class BasicListener : IListener
    {
        public string? LastContent { get; private set; }
        public void ReceiveConfigInfo(string configInfo) => LastContent = configInfo;
    }
}
