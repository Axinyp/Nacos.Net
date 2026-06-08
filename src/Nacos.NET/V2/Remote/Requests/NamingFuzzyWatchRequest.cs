namespace Nacos.V2.Remote.Requests
{
    using System.Collections.Generic;

    public class NamingFuzzyWatchRequest : CommonRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("namespace")]
        public string Namespace { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("groupName")]
        public string GroupName { get; set; }

        /// <summary>Service key patterns to watch, each formatted as "groupName@@serviceName".</summary>
        [System.Text.Json.Serialization.JsonPropertyName("watchedServiceList")]
        public List<string> WatchedServiceList { get; set; } = new List<string>();

        /// <summary>"ADD_WATCH" to subscribe, "DELETE_WATCH" to unsubscribe.</summary>
        [System.Text.Json.Serialization.JsonPropertyName("syncType")]
        public string SyncType { get; set; }

        public override string GetRemoteType() => RemoteRequestType.Req_Naming_FuzzyWatch;
    }
}
