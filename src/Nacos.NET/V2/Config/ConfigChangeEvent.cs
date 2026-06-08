namespace Nacos.V2.Config
{
    using System.Collections.Generic;

    public class ConfigChangeEvent
    {
        public string DataId { get; }
        public string Group { get; }
        public string Namespace { get; }
        public IReadOnlyDictionary<string, ConfigChangeItem> Changes { get; }

        public ConfigChangeEvent(
            string dataId,
            string group,
            string @namespace,
            IReadOnlyDictionary<string, ConfigChangeItem> changes)
        {
            DataId = dataId;
            Group = group;
            Namespace = @namespace;
            Changes = changes;
        }
    }
}
