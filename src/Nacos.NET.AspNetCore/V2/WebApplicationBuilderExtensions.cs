namespace Microsoft.AspNetCore.Builder;

using Microsoft.Extensions.Configuration;
using Nacos.AspNetCore.V2;

/// <summary>
/// .NET 10 一键注册 Nacos 配置中心 + 服务注册/发现。
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// 同时注册 Nacos 配置中心（<c>IConfiguration</c> 热更新）和服务命名（向 Nacos 注册当前实例）。
    ///
    /// <para>等价于手动调用：</para>
    /// <code>
    /// builder.Host.UseNacosConfig(section);
    /// builder.Services.AddNacosAspNet(builder.Configuration, section);
    /// </code>
    /// </summary>
    /// <param name="builder">WebApplicationBuilder</param>
    /// <param name="section">appsettings.json 中 Nacos 配置节名，默认 <c>NacosConfig</c></param>
    /// <returns>同一 <see cref="WebApplicationBuilder"/> 实例，支持链式调用</returns>
    public static WebApplicationBuilder AddNacos(this WebApplicationBuilder builder, string section = "NacosConfig")
    {
        // 1. 挂载配置中心：在 Host 构建阶段将 Nacos 配置合并到 IConfiguration，支持热更新
        builder.Host.UseNacosConfig(section);

        // 2. 注册命名服务：启动时向 Nacos 注册当前实例，关闭时自动注销
        builder.Services.AddNacosAspNet(builder.Configuration, section);

        return builder;
    }
}
