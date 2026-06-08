using Nacos.V2.Config;
using Nacos.V2.Naming;
using Nacos.V2.Naming.Dtos;
using System.Collections.Generic;

namespace ConferenceAIOServer.Nacos.Tests.Unit;

/// <summary>
/// ConfigChangeEvent / ConfigChangeItem / FuzzyWatchChangeEvent 值类型单元测试
/// </summary>
public class NacosEventModelTests
{
    // ── ConfigChangeItem ─────────────────────────────────────────────────────

    [Fact]
    public void ConfigChangeItem_Properties_Are_Correct()
    {
        var item = new ConfigChangeItem("myKey", "oldVal", "newVal", ConfigChangeType.Modified);
        Assert.Equal("myKey",   item.Key);
        Assert.Equal("oldVal",  item.OldValue);
        Assert.Equal("newVal",  item.NewValue);
        Assert.Equal(ConfigChangeType.Modified, item.Type);
    }

    [Fact]
    public void ConfigChangeItem_Added_Has_Null_OldValue()
    {
        var item = new ConfigChangeItem("k", null, "v", ConfigChangeType.Added);
        Assert.Null(item.OldValue);
        Assert.NotNull(item.NewValue);
    }

    [Fact]
    public void ConfigChangeItem_Deleted_Has_Null_NewValue()
    {
        var item = new ConfigChangeItem("k", "v", null, ConfigChangeType.Deleted);
        Assert.Null(item.NewValue);
        Assert.NotNull(item.OldValue);
    }

    // ── ConfigChangeEvent ─────────────────────────────────────────────────────

    [Fact]
    public void ConfigChangeEvent_Properties_Are_Correct()
    {
        var changes = new Dictionary<string, ConfigChangeItem>
        {
            ["x"] = new ConfigChangeItem("x", "1", "2", ConfigChangeType.Modified)
        };

        var evt = new ConfigChangeEvent("my-data-id", "DEFAULT_GROUP", "dev", changes);

        Assert.Equal("my-data-id",     evt.DataId);
        Assert.Equal("DEFAULT_GROUP",  evt.Group);
        Assert.Equal("dev",            evt.Namespace);
        Assert.Single(evt.Changes);
        Assert.True(evt.Changes.ContainsKey("x"));
    }

    [Fact]
    public void ConfigChangeEvent_Changes_Is_ReadOnly()
    {
        var changes = new Dictionary<string, ConfigChangeItem>();
        var evt = new ConfigChangeEvent("d", "g", "n", changes);

        // IReadOnlyDictionary should not expose mutating Add
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, ConfigChangeItem>>(evt.Changes);
    }

    // ── FuzzyWatchChangeEvent ────────────────────────────────────────────────

    [Fact]
    public void FuzzyWatchChangeEvent_Properties_Are_Correct()
    {
        var svcInfo = new ServiceInfo();
        var evt = new FuzzyWatchChangeEvent("dev", "DEFAULT_GROUP", "my-service", svcInfo, "ADD");

        Assert.Equal("dev",             evt.Namespace);
        Assert.Equal("DEFAULT_GROUP",   evt.GroupName);
        Assert.Equal("my-service",      evt.ServiceName);
        Assert.Same(svcInfo,            evt.ServiceInfo);
        Assert.Equal("ADD",             evt.SyncType);
    }

    [Theory]
    [InlineData("ADD")]
    [InlineData("DELETE")]
    [InlineData("CHANGED")]
    public void FuzzyWatchChangeEvent_All_SyncTypes_Are_Accepted(string syncType)
    {
        var evt = new FuzzyWatchChangeEvent("ns", "g", "s", null, syncType);
        Assert.Equal(syncType, evt.SyncType);
    }

    // ── ConfigChangeType enum ─────────────────────────────────────────────────

    [Fact]
    public void ConfigChangeType_All_Values_Defined()
    {
        var values = System.Enum.GetValues<ConfigChangeType>();
        Assert.Contains(ConfigChangeType.Added,    values);
        Assert.Contains(ConfigChangeType.Modified, values);
        Assert.Contains(ConfigChangeType.Deleted,  values);
    }
}
