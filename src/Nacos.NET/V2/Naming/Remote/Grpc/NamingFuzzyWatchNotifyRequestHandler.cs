namespace Nacos.V2.Naming.Remote.Grpc
{
    using Microsoft.Extensions.Logging;
    using Nacos.V2.Remote;
    using Nacos.V2.Remote.Requests;
    using Nacos.V2.Remote.Responses;
    using System;
    using System.Collections.Concurrent;

    public class NamingFuzzyWatchNotifyRequestHandler : IServerRequestHandler
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<IFuzzyWatcher, byte> _watchers = new ConcurrentDictionary<IFuzzyWatcher, byte>();

        public NamingFuzzyWatchNotifyRequestHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void AddWatcher(IFuzzyWatcher watcher) => _watchers.TryAdd(watcher, 0);

        public void RemoveWatcher(IFuzzyWatcher watcher) => _watchers.TryRemove(watcher, out _);

        public CommonResponse RequestReply(CommonRequest request)
        {
            if (request is not NamingFuzzyWatchNotifyRequest notifyRequest)
                return null;

            var changeEvent = new FuzzyWatchChangeEvent(
                notifyRequest.Namespace,
                notifyRequest.GroupName,
                notifyRequest.ServiceName,
                notifyRequest.ServiceInfo,
                notifyRequest.SyncType);

            foreach (var watcher in _watchers.Keys)
            {
                try
                {
                    watcher.OnChange(changeEvent);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "[NamingFuzzyWatchNotifyRequestHandler] Error notifying watcher");
                }
            }

            return new NamingFuzzyWatchNotifyResponse();
        }
    }
}
