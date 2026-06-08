namespace Nacos.V2.Remote.Requests
{
    public class ConfigFuzzyWatchRequest : CommonRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("namespace")]
        public string Namespace { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("group")]
        public string Group { get; set; }

        /// <summary>DataId pattern to watch (supports '*' wildcard).</summary>
        [System.Text.Json.Serialization.JsonPropertyName("dataId")]
        public string DataId { get; set; }

        /// <summary>"ADD_WATCH" to subscribe, "DELETE_WATCH" to unsubscribe.</summary>
        [System.Text.Json.Serialization.JsonPropertyName("syncType")]
        public string SyncType { get; set; }

        public override string GetRemoteType() => RemoteRequestType.Req_Config_FuzzyWatch;
    }
}
