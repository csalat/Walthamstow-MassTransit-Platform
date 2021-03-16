using System;
using System.Collections.Generic;
using System.Linq;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core.Topology;
using MassTransit.PrometheusIntegration;
using MassTransit.RabbitMqTransport.Topology;
using Serilog;

namespace Walthamstow.MassTransit.Platform.Startup
{
    
        public class PlatformOptions
    {
        public const string RabbitMq = "rabbitmq";
        public const string RMQ = "rmq";
        public const string AzureServiceBus = "servicebus";
        public const string ASB = "asb";
        
        public const string Mediator = "mediator";
        public PlatformOptions()
        {
            Transport = Mediator;
        }

        public string Transport { get; set; }

        /// <summary>
        /// If specified, Prometheus metrics are enabled for the specified service name
        /// </summary>
        public string Prometheus { get; set; }

        /// <summary>
        /// If specified, is the queue name of the endpoint where the message scheduler is running (if using Quartz or HangFire)
        /// </summary>
        public string Scheduler { get; set; }

        public bool TryGetSchedulerEndpointAddress(out Uri address)
        {
            if (!string.IsNullOrWhiteSpace(Scheduler))
            {
                if (Uri.IsWellFormedUriString(Scheduler, UriKind.Absolute))
                {
                    address = new Uri(Scheduler);
                    return true;
                }

                switch (Transport.ToLowerInvariant())
                {
                    case RabbitMq when RabbitMqEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                    case RMQ when RabbitMqEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                        address = new Uri($"exchange:{Scheduler}");
                        return true;

                    case AzureServiceBus when ServiceBusEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                    case ASB when ServiceBusEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                        address = new Uri($"queue:{Scheduler}");
                        return true;
                    
                    case Mediator:
                        address = default;
                        return true;
                }
            }

            address = default;
            return false;
        }
    }

    
    public class StartupBusConfigurator :
        IStartupBusConfigurator
    {
        readonly PlatformOptions _platformOptions;
        readonly Uri _schedulerEndpointAddress;

        public StartupBusConfigurator(PlatformOptions platformOptions)
        {
            _platformOptions = platformOptions;

            _platformOptions.TryGetSchedulerEndpointAddress(out _schedulerEndpointAddress);
        }

        public bool HasSchedulerEndpoint => _schedulerEndpointAddress != null;

        public void ConfigureBus<TEndpointConfigurator>(IBusFactoryConfigurator<TEndpointConfigurator> configurator, IBusRegistrationContext context)
            where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
            configurator.UseHealthCheck(context);

            if (!string.IsNullOrWhiteSpace(_platformOptions.Prometheus))
            {
                Log.Information("Configuring Prometheus Metrics: {ServiceName}", _platformOptions.Prometheus);

                configurator.UsePrometheusMetrics(serviceName: _platformOptions.Prometheus);
            }

            List<IPlatformStartup> hostingConfigurators = context.GetService<IEnumerable<IPlatformStartup>>()?.ToList();

            foreach (var hostingConfigurator in hostingConfigurators)
                hostingConfigurator.ConfigureBus(configurator, context);

            configurator.ConfigureEndpoints(context);
        }

        public bool TryConfigureQuartz(IBusFactoryConfigurator configurator)
        {
            if (_schedulerEndpointAddress == null)
                return false;

            Log.Information("Configuring Quartz Message Scheduler (endpoint: {QuartzAddress}", _schedulerEndpointAddress);
            configurator.UseMessageScheduler(_schedulerEndpointAddress);
            return true;
        }
    }
}