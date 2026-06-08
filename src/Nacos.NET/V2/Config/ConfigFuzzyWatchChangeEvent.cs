namespace Nacos.V2.Config
{
    public class ConfigFuzzyWatchChangeEvent
    {
        public string Namespace { get; }
        public string Group { get; }
        public string DataId { get; }
        public string Content { get; }

        /// <summary>ADD_CONFIG, DELETE_CONFIG, or CHANGE_CONFIG</summary>
        public string SyncType { get; }

        public ConfigFuzzyWatchChangeEvent(string @namespace, string group, string dataId, string content, string syncType)
        {
            Namespace = @namespace;
            Group = group;
            DataId = dataId;
            Content = content;
            SyncType = syncType;
        }
    }
}
