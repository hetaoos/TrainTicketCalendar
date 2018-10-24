using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// ServiceCollectionExtensions
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册一个包含IHostedService的单实例服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <param name="services">服务对象</param>
        /// <returns></returns>
        public static IServiceCollection AddHostedServiceEx<TService>(this IServiceCollection services)
            where TService : class, IHostedService
        {
            return services.AddSingleton<TService>()
                   .AddSingleton<IHostedService>(o => o.GetService<TService>());
        }

        /// <summary>
        /// 注册一个包含IHostedService的单实例服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="services">服务对象</param>
        /// <returns></returns>
        public static IServiceCollection AddHostedServiceEx<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService, IHostedService
        {
            return services.AddSingleton<TService, TImplementation>()
                 .AddSingleton<IHostedService>(o => o.GetServices<TService>().OfType<TImplementation>().FirstOrDefault());
        }
    }
}