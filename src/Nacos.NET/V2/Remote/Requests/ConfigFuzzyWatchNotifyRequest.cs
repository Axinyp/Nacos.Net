namespace Nacos.V2.Remote.Requests
{
    /// <summary>Server-push request notifying the client that a watched config has changed.</summary>
    public class ConfigFuzzyWatchNotifyRequest : CommonRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("namespace")]
        public string Namespace { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("group")]
        public string Group { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("dataId")]
        public string DataId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string Content { get; set; }

        /// <summary>"ADD_CONFIG", "DELETE_CONFIG", or "CHANGE_CONFIG"</summary>
        [System.Text.Json.Serialization.JsonPropertyName("syncType")]
        public string SyncType { get; set; }

        public override string GetRemoteType() => RemoteRequestType.Req_Config_FuzzyWatchNotify;
    }
}
