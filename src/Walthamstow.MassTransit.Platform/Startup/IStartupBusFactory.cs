using MassTransit.ExtensionsDependencyInjectionIntegration;

namespace Walthamstow.MassTransit.Platform.Startup
{
    public interface IStartupBusFactory
    {
        void CreateBus(IServiceCollectionBusConfigurator busConfigurator, IStartupBusConfigurator configurator);
    }
}