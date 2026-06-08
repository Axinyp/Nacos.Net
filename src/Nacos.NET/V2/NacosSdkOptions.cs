namespace Nacos.V2
{
    using System.Collections.Generic;

    public class NacosSdkOptions
    {
        /// <summary>
        /// nacos server addresses.
        /// </summary>
        /// <example>
        /// http://10.1.12.123:8848,https://10.1.12.124:8848
        /// </example>
        public List<string> ServerAddresses { get; set; }

        /// <summary>
        /// EndPoint
        /// </summary>
        public string EndPoint { get; set; }

        public string ContextPath { get; set; } = "nacos";

        /// <summary>
        /// default timeout, unit is Milliseconds.
        /// </summary>
        public int DefaultTimeOut { get; set; } = 15000;

        /// <summary>
        /// default namespace
        /// </summary>
        public string Namespace { get; set; } = "";

        /// <summary>
        /// accessKey
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// secretKey
        /// </summary>
        public string SecretKey { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string RamRoleName { get; set; }

        /// <summary>
        /// listen interval, unit is millisecond.
        /// </summary>
        public int ListenInterval { get; set; } = 1000;

        [Obsolete("HTTP v1 API was removed in Nacos 3.2. Setting this to false will break connectivity with Nacos ≥ 3.2 servers. " +
                  "This option will be removed in a future release. Default (true) uses gRPC and is safe for all Nacos versions.")]
        public bool ConfigUseRpc { get; set; } = true;

        [Obsolete("HTTP v1 API was removed in Nacos 3.2. Setting this to false will break connectivity with Nacos ≥ 3.2 servers. " +
                  "This option will be removed in a future release. Default (true) uses gRPC and is safe for all Nacos versions.")]
        public bool NamingUseRpc { get; set; } = true;

        public string NamingLoadCacheAtStart { get; set; }

        public string NamingCacheRegistryDir { get; set; }

        /// <summary>
        /// Whether enable protecting naming push empty data, default is false.
        /// </summary>
        public bool NamingPushEmptyProtection { get; set; } = false;

        /// <summary>
        /// Specify the assemblies that contains the impl of IConfigFilter.
        /// </summary>
        public List<string> ConfigFilterAssemblies { get; set; }

        /// <summary>
        /// Specify some extension info of IConfigFilter.
        /// </summary>
        public string ConfigFilterExtInfo { get; set; }

        /// <summary>
        /// TLS config
        /// </summary>
        public TLSConfig TLSConfig { get; set; }

        /// <summary>
        /// gRPC keepalive settings for long-lived connections.
        /// </summary>
        public GrpcKeepaliveOptions GrpcKeepalive { get; set; } = new();

        /// <summary>
        /// Default heartbeat interval for HTTP naming (milliseconds). Defaults to 5000.
        /// </summary>
        public int HeartBeatInterval { get; set; } = 5000;

        /// <summary>
        /// Timeout waiting for gRPC ability negotiation on connect (milliseconds). Defaults to 3000.
        /// </summary>
        public int CapabilityNegotiationTimeout { get; set; } = 3000;

        /// <summary>
        /// Number of retries for failed requests. Defaults to 3.
        /// </summary>
        public int RetryTimes { get; set; } = 3;

        /// <summary>
        /// Application name reported to Nacos server.
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// Whether to enable client-side metrics collection. Defaults to false.
        /// </summary>
        public bool EnableClientMetrics { get; set; } = false;
    }

    public class GrpcKeepaliveOptions
    {
        /// <summary>
        /// How often to send a keepalive ping. Defaults to 30 s.
        /// </summary>
        public TimeSpan PingDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Timeout waiting for a keepalive ping ack before closing the connection. Defaults to 10 s.
        /// </summary>
        public TimeSpan PingTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Initial per-stream flow control window size in bytes. 0 = use channel default.
        /// </summary>
        public int InitialStreamWindowSize { get; set; } = 0;

        /// <summary>
        /// Initial connection-level flow control window size in bytes. 0 = use channel default.
        /// </summary>
        public int InitialConnectionWindowSize { get; set; } = 0;
    }
}
