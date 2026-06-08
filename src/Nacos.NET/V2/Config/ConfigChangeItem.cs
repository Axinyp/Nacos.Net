namespace Nacos.V2.Config
{
    public class ConfigChangeItem
    {
        public string Key { get; }
        public string? OldValue { get; }
        public string? NewValue { get; }
        public ConfigChangeType Type { get; }

        public ConfigChangeItem(string key, string? oldValue, string? newValue, ConfigChangeType type)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
            Type = type;
        }

        public override string ToString()
            => $"ConfigChangeItem [key={Key}, oldValue={OldValue}, newValue={NewValue}, type={Type}]";
    }
}
