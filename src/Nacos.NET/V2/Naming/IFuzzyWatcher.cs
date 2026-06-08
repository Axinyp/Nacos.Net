namespace Nacos.V2.Naming
{
    public interface IFuzzyWatcher
    {
        void OnChange(FuzzyWatchChangeEvent changeEvent);
    }
}
