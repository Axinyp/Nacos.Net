namespace Nacos.V2.Naming
{
    using Nacos.V2.Naming.Dtos;

    public class FuzzyWatchChangeEvent
    {
        public string Namespace { get; }
        public string GroupName { get; }
        public string ServiceName { get; }
        public ServiceInfo ServiceInfo { get; }

        /// <summary>ADD, DELETE, or CHANGED</summary>
        public string SyncType { get; }

        public FuzzyWatchChangeEvent(string @namespace, string groupName, string serviceName, ServiceInfo serviceInfo, string syncType)
        {
            Namespace = @namespace;
            GroupName = groupName;
            ServiceName = serviceName;
            ServiceInfo = serviceInfo;
            SyncType = syncType;
        }
    }
}
