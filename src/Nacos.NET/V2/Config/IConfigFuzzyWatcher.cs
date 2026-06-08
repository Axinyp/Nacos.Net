namespace Nacos.V2.Config
{
    public interface IConfigFuzzyWatcher
    {
        void OnChange(ConfigFuzzyWatchChangeEvent changeEvent);
    }
}
