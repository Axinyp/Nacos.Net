namespace Nacos.V2
{
    using Nacos.V2.Config;

    /// <summary>
    /// Enhanced config listener that receives structured change events in addition to the raw config string.
    /// Implement this instead of <see cref="IListener"/> to get per-key diff information.
    /// </summary>
    public interface IConfigChangeListener : IListener
    {
        /// <summary>
        /// Called after <see cref="IListener.ReceiveConfigInfo"/> when the content has changed.
        /// Contains a per-key diff between the previous and current config values.
        /// </summary>
        void ReceiveConfigChange(ConfigChangeEvent changeEvent);
    }
}
