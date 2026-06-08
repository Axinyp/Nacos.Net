namespace Nacos.V2
{
    using Nacos.V2.Config;
    using System.Threading.Tasks;

    public interface INacosConfigService
    {
        /// <summary>
        /// Get config.
        /// </summary>
        /// <param name="dataId">dataId</param>
        /// <param name="group">group</param>
        /// <param name="timeoutMs">read timeout</param>
        /// <returns>config value</returns>
        Task<string> GetConfig(string dataId, string group, long timeoutMs);

        /// <summary>
        /// Get config and register Listener.
        /// If you want to pull it yourself when the program starts to get the configuration for the first time, and the
        /// registered Listener is used for future configuration updates, you can keep the original code unchanged, just add
        /// the system parameter: enableRemoteSyncConfig = "true" ( But there is network overhead); therefore we recommend
        /// that you use this interface directly
        /// </summary>
        /// <param name="dataId">dataId</param>
        /// <param name="group">group</param>
        /// <param name="timeoutMs">read timeout</param>
        /// <param name="listener">Listener</param>
        /// <returns>config value</returns>
        Task<string> GetConfigAndSignListener(string dataId, string group, long timeoutMs, IListener listener);

        /// <summary>
        /// Add a listener to the configuration, after the server modified the configuration, the client will use the
        /// incoming listener callback. Recommended asynchronous processing, the application can implement the getExecutor
        /// method in the ManagerListener, provide a thread pool of execution. If provided, use the main thread callback, May
        /// block other configurations or be blocked by other configurations.
        /// </summary>
        /// <param name="dataId">dataId</param>
        /// <param name="group">group</param>
        /// <param name="listener">Listener</param>
        Task AddListener(string dataId, string group, IListener listener);

        /// <summary>
        /// Publish config.
        /// </summary>
        /// <param name="dataId">dataId</param>
        /// <param name="group">group</param>
        /// <param name="content">content</param>
        /// <returns>Whether publish</returns>
        Task<bool> PublishConfig(string dataId, string group, string content);

        /// <summary>
        /// Publish config.
        /// </summary>
        /// <param name="dataId">dataId</param>
        /// <param name="group">group</param>
        /// <param name="content">content</param>
        /// <param name="type">config type</param>
        /// <returns>Whether publish</returns>
        Task<bool> PublishConfig(string dataId, string group, string content, string type);

        /// <summary>
        /// Publish config.
        /// </summary>
        /// <param name="dataId">dataId</param>
        /// <param name="group">group</param>
        /// <param name="content">content</param>
        /// <param name="casMd5">casMd5 prev content's md5 to cas</param>
        /// <returns>Whether publish</returns>
        Task<bool> PublishConfigCas(string dataId, string group, string content, string casMd5);

        /// <summary>
        /// Publish config.
        /// </summary>
        /// <param name="dataId">dataId</param>
        /// <param name="group">group</param>
        /// <param name="content">content</param>
        /// <param name="casMd5">casMd5 prev content's md5 to cas</param>
        /// <param name="type">config type</param>
        /// <returns>Whether publish</returns>
        Task<bool> PublishConfigCas(string dataId, string group, string content, string casMd5, string type);

        /// <summary>
        /// Remove config.
        /// </summary>
        /// <param name="dataId">dataId</param>
        /// <param name="group">group</param>
        /// <returns>whether remove</returns>
        Task<bool> RemoveConfig(string dataId, string group);

        /// <summary>
        /// Remove listener.
        /// </summary>
        /// <param name="dataId">dataId</param>
        /// <param name="group">group</param>
        /// <param name="listener">listener</param>
        Task RemoveListener(string dataId, string group, IListener listener);

        /// <summary>
        /// Get server status.
        /// </summary>
        /// <returns>whether health</returns>
        Task<string> GetServerStatus();

        /// <summary>
        /// Shutdown the resource service.
        /// </summary>
        Task ShutDown();

        /// <summary>
        /// Watch configs matching a dataId pattern in the given group.
        /// Requires Nacos 3.x server with ConfigFuzzyWatch ability enabled.
        /// </summary>
        /// <param name="group">config group</param>
        /// <param name="dataIdPattern">dataId pattern, supports '*' wildcard</param>
        /// <param name="watcher">watcher callback</param>
        Task FuzzyWatchConfig(string group, string dataIdPattern, IConfigFuzzyWatcher watcher);

        /// <summary>
        /// Unwatch configs matching a dataId pattern.
        /// </summary>
        /// <param name="group">config group</param>
        /// <param name="dataIdPattern">dataId pattern</param>
        /// <param name="watcher">watcher to remove</param>
        Task FuzzyUnwatchConfig(string group, string dataIdPattern, IConfigFuzzyWatcher watcher);

        // TODO(N-3x-1): When Nacos 3.2+ removes v1/v2 HTTP APIs, the HTTP transport path
        //   (ConfigHttpTransportClient / ServerHttpAgent) must be migrated to v3 REST or
        //   forced to gRPC-only mode. Affects: V2/Config/Http/*.cs
    }
}