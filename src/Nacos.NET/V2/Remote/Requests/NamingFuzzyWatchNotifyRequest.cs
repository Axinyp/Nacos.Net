namespace Nacos.V2.Remote.Requests
{
    /// <summary>Server-push request notifying the client that a watched service has changed.</summary>
    public class NamingFuzzyWatchNotifyRequest : CommonRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("namespace")]
        public string Namespace { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("groupName")]
        public string GroupName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("serviceName")]
        public string ServiceName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("serviceInfo")]
        public Nacos.V2.Naming.Dtos.ServiceInfo ServiceInfo { get; set; }

        /// <summary>"ADD", "DELETE", or "CHANGED"</summary>
        [System.Text.Json.Serialization.JsonPropertyName("syncType")]
        public string SyncType { get; set; }

        public override string GetRemoteType() => RemoteRequestType.Req_Naming_FuzzyWatchNotify;
    }
}
