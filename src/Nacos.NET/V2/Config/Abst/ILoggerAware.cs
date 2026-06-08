namespace Nacos.V2.Config.Abst;

using Microsoft.Extensions.Logging;

/// <summary>
/// Optional interface for <see cref="IConfigFilter"/> implementations that support structured logging.
/// <see cref="FilterImpl.ConfigFilterChainManager"/> will call <see cref="SetLogger"/> after instantiation
/// if the filter implements this interface.
/// </summary>
public interface ILoggerAware
{
    void SetLogger(ILogger logger);
}
