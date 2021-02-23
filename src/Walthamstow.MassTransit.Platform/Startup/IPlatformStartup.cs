using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Walthamstow.MassTransit.Platform.Startup
{
    /// <summary>
    /// Implemented by a class used to startup up the MassTransit platform
    /// </summary>
    public interface IPlatformStartup
    {
        /// <summary>
        /// Configure MassTransit, using the supplied configurators
        /// </summary>
        /// <param name="configurator">Use to configure consumers, sagas, activities, request clients, etc.</param>
        /// <param name="services">Use to add dependencies to the container</param>
        void ConfigureMassTransit(IServiceCollectionBusConfigurator configurator, IServiceCollection services);

        /// <summary>
        /// Configure the bus, using the supplied configurators
        /// </summary>
        /// <param name="configurator">Can be used to specify additional bus configuration</param>
        /// <param name="context">The service provider, can be used to resolve dependencies</param>
        void ConfigureBus<TEndpointConfigurator>(IBusFactoryConfigurator<TEndpointConfigurator> configurator, IBusRegistrationContext context)
            where TEndpointConfigurator : IReceiveEndpointConfigurator;

        void Configure(IApplicationBuilder app, IWebHostEnvironment env);
        
    }
}