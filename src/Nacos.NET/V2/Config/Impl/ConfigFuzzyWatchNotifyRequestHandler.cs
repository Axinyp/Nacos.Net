namespace Nacos.V2.Config.Impl
{
    using Microsoft.Extensions.Logging;
    using Nacos.V2.Remote;
    using Nacos.V2.Remote.Requests;
    using Nacos.V2.Remote.Responses;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public class ConfigFuzzyWatchNotifyRequestHandler : IServerRequestHandler
    {
        private readonly ILogger _logger;

        // Key: "group@@dataIdPattern" → set of watchers subscribed to that exact pattern
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<IConfigFuzzyWatcher, byte>> _subscriptions
            = new ConcurrentDictionary<string, ConcurrentDictionary<IConfigFuzzyWatcher, byte>>();

        public ConfigFuzzyWatchNotifyRequestHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void AddWatcher(string group, string dataIdPattern, IConfigFuzzyWatcher watcher)
        {
            _subscriptions
                .GetOrAdd(MakeKey(group, dataIdPattern), _ => new ConcurrentDictionary<IConfigFuzzyWatcher, byte>())
                .TryAdd(watcher, 0);
        }

        public void RemoveWatcher(string group, string dataIdPattern, IConfigFuzzyWatcher watcher)
        {
            var key = MakeKey(group, dataIdPattern);
            if (!_subscriptions.TryGetValue(key, out var set)) return;
            set.TryRemove(watcher, out _);
            if (set.IsEmpty) _subscriptions.TryRemove(key, out _);
        }

        /// <summary>Returns all (group, dataIdPattern) pairs that have at least one active watcher.</summary>
        public IEnumerable<(string group, string pattern)> GetWatchedPatterns()
            => _subscriptions.Keys.Select(ParseKey);

        public CommonResponse RequestReply(CommonRequest request)
        {
            if (request is not ConfigFuzzyWatchNotifyRequest n)
                return null;

            var key = MakeKey(n.Group, n.DataId);
            if (_subscriptions.TryGetValue(key, out var set))
            {
                var changeEvent = new ConfigFuzzyWatchChangeEvent(n.Namespace, n.Group, n.DataId, n.Content, n.SyncType);
                foreach (var watcher in set.Keys)
                {
                    try
                    {
                        watcher.OnChange(changeEvent);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "[ConfigFuzzyWatch] Error notifying watcher for {0}", key);
                    }
                }
            }

            return new ConfigFuzzyWatchNotifyResponse();
        }

        private static string MakeKey(string group, string pattern) => $"{group}@@{pattern}";

        private static (string group, string pattern) ParseKey(string key)
        {
            var idx = key.IndexOf("@@", StringComparison.Ordinal);
            return idx >= 0 ? (key[..idx], key[(idx + 2)..]) : (key, string.Empty);
        }
    }
}
