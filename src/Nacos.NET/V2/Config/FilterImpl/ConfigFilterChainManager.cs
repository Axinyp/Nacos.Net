namespace Nacos.V2.Config.FilterImpl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Nacos.V2.Config.Abst;

    public class ConfigFilterChainManager : IConfigFilterChain
    {
        private List<IConfigFilter> filters = new List<IConfigFilter>();

        public ConfigFilterChainManager(NacosSdkOptions options, ILoggerFactory? loggerFactory = null)
        {
            var logger = loggerFactory?.CreateLogger<ConfigFilterChainManager>()
                         ?? NullLogger<ConfigFilterChainManager>.Instance;

            var assemblies = GetAssemblies(options, logger);
            var configFilters = new List<IConfigFilter>();
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    logger.LogWarning(ex,
                        "[Nacos] Partial type load failure in assembly {Assembly}; skipping unloadable types.",
                        assembly.GetName().Name);
                    types = ex.Types.Where(t => t != null).ToArray()!;
                }

                foreach (var type in types)
                {
                    if (type is null || type.IsAbstract || !type.GetInterfaces().Contains(typeof(IConfigFilter)))
                        continue;

                    try
                    {
                        configFilters.Add((IConfigFilter)Activator.CreateInstance(type)!);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "[Nacos] Failed to instantiate IConfigFilter '{Type}'. " +
                            "Ensure it has a public parameterless constructor.",
                            type.FullName);
                    }
                }
            }

            if (configFilters.Count == 0)
            {
                if (options.ConfigFilterAssemblies?.Count > 0)
                    logger.LogWarning("[Nacos] No IConfigFilter found in assemblies [{Assemblies}]. " +
                                      "Verify the assembly is referenced and the DLL is in the output directory.",
                        string.Join(", ", options.ConfigFilterAssemblies));
                else
                    logger.LogDebug("[Nacos] ConfigFilterAssemblies is empty; no config filters registered.");
            }

            foreach (var configFilter in configFilters)
            {
                if (configFilter is ILoggerAware aware)
                    aware.SetLogger(loggerFactory?.CreateLogger(configFilter.GetType())
                                    ?? NullLogger.Instance);

                configFilter.Init(options);
                AddFilter(configFilter);
                logger.LogInformation("[Nacos] Config filter registered: {FilterName} (order={Order})",
                    configFilter.GetFilterName(), configFilter.GetOrder());
            }
        }

        private static List<Assembly> GetAssemblies(NacosSdkOptions options, ILogger logger)
        {
            var result = new List<Assembly>();

            if (options.ConfigFilterAssemblies == null || options.ConfigFilterAssemblies.Count == 0)
                return result;

            // Already-loaded assemblies (covers project references in standard and single-file deployments).
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .ToDictionary(a => a.GetName().Name!, StringComparer.OrdinalIgnoreCase);

            foreach (var name in options.ConfigFilterAssemblies)
            {
                if (loaded.TryGetValue(name, out var found))
                {
                    result.Add(found);
                    logger.LogDebug("[Nacos] Filter assembly resolved from AppDomain: {Assembly}", name);
                }
                else
                {
                    try
                    {
                        var asm = Assembly.Load(new AssemblyName(name));
                        result.Add(asm);
                        logger.LogDebug("[Nacos] Filter assembly loaded explicitly: {Assembly}", name);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "[Nacos] Filter assembly not found: {Assembly}. " +
                                              "Ensure the project is referenced so the DLL is deployed.", name);
                    }
                }
            }

            return result;
        }

        public void DoFilter(IConfigRequest request, IConfigResponse response)
        {
            new VirtualFilterChain(this.filters).DoFilter(request, response);
        }

        public ConfigFilterChainManager AddFilter(IConfigFilter filter)
        {
            // 根据order大小顺序插入
            int i = 0;
            while (i < this.filters.Count)
            {
                IConfigFilter currentValue = this.filters[i];
                if (currentValue.GetFilterName().Equals(filter.GetFilterName()))
                {
                    break;
                }

                if (filter.GetOrder() >= currentValue.GetOrder() && i < this.filters.Count)
                {
                    i++;
                }
                else
                {
                    this.filters.Insert(i, filter);
                    break;
                }
            }

            if (i == this.filters.Count)
            {
                this.filters.Insert(i, filter);
            }

            return this;
        }

        internal class VirtualFilterChain : IConfigFilterChain
        {
            private readonly List<IConfigFilter> additionalFilters;

            private int currentPosition = 0;

            public VirtualFilterChain(List<IConfigFilter> additionalFilters)
            {
                this.additionalFilters = additionalFilters;
            }

            public void DoFilter(IConfigRequest request, IConfigResponse response)
            {
                if (this.currentPosition != this.additionalFilters.Count)
                {
                    this.currentPosition++;
                    IConfigFilter nextFilter = this.additionalFilters[this.currentPosition - 1];
                    nextFilter.DoFilter(request, response, this);
                }
            }
        }
    }
}
