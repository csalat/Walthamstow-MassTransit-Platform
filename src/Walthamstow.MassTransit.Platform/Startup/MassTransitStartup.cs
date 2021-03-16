using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Definition;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prometheus;
using Serilog;
using Walthamstow.MassTransit.Platform.SagaConfig;
using Walthamstow.MassTransit.Platform.Startup.RabbitMq;
using Walthamstow.MassTransit.Platform.Startup.ServiceBus;

namespace Walthamstow.MassTransit.Platform.Startup
{
    public class MassTransitStartup
    {
        public MassTransitStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Log.Information("Configuring MassTransit Services");
            services.AddHealthChecks();
            services.Configure<PlatformOptions>(Configuration);

            RabbitMqStartupBusFactory.Configure(services, Configuration);
            ServiceBusStartupBusFactory.Configure(services, Configuration);

            var configurationServiceProvider = services.BuildServiceProvider();
            
            services.ConfigureSagaDbs(Configuration);
            
            List<IPlatformStartup> platformStartups = configurationServiceProvider.
                GetService<IEnumerable<IPlatformStartup>>()?.ToList();

            ConfigureApplicationInsights(services);

            services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
            services.AddMassTransit(cfg =>
            {
                foreach (var platformStartup in platformStartups)
                    platformStartup.ConfigurePlatform(cfg, services, Configuration);

                CreateBus(cfg, configurationServiceProvider);
            });

            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(2);
            });
            
            services.AddMassTransitHostedService();
        }

        void ConfigureApplicationInsights(IServiceCollection services)
        {
            if (string.IsNullOrWhiteSpace(Configuration.GetSection("ApplicationInsights")?.GetValue<string>("InstrumentationKey")))
                return;

            Log.Information("Configuring Application Insights");

            services.AddApplicationInsightsTelemetry();

            services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
            {
                module.IncludeDiagnosticSourceActivities.Add("MassTransit");
            });
        }

        void CreateBus(IServiceCollectionBusConfigurator busConfigurator, IServiceProvider provider)
        {
            var platformOptions = provider.GetRequiredService<IOptions<PlatformOptions>>().Value;

            var configurator = new StartupBusConfigurator(platformOptions);

            switch (platformOptions.Transport.ToLower(CultureInfo.InvariantCulture))
            {
                case PlatformOptions.RabbitMq:
                case PlatformOptions.RMQ:
                    new RabbitMqStartupBusFactory().CreateBus(busConfigurator, configurator);
                    break;

                case PlatformOptions.AzureServiceBus:
                case PlatformOptions.ASB:
                    new ServiceBusStartupBusFactory().CreateBus(busConfigurator, configurator);
                    break;
                
                case PlatformOptions.Mediator:
                    break;
                default:
                    throw new ConfigurationException($"Unknown transport type: {platformOptions.Transport}");
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            // here we execute our own startup

            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            List<IPlatformStartup> platformStartups = app.ApplicationServices.GetRequiredService<IEnumerable<IPlatformStartup>>()?.ToList();
            foreach (var platformStartup in platformStartups)
                platformStartup.Configure(app,env);
            
            var platformOptions = app.ApplicationServices.GetRequiredService<IOptions<PlatformOptions>>().Value;

            app.UseHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = HealthCheckResponseWriter
            });
            app.UseHealthChecks("/health/live", new HealthCheckOptions {ResponseWriter = HealthCheckResponseWriter});

            if (!string.IsNullOrWhiteSpace(platformOptions.Prometheus))
            {
                Log.Information("Configuring Prometheus Endpoint: /metrics");

                app.UseMetricServer();
            }
        }

        static Task HealthCheckResponseWriter(HttpContext context, HealthReport result)
        {
            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Entries.Select(entry => new JProperty(entry.Key, new JObject(
                    new JProperty("status", entry.Value.Status.ToString()),
                    new JProperty("description", entry.Value.Description),
                    new JProperty("data", JObject.FromObject(entry.Value.Data))))))));

            context.Response.ContentType = "application/json";

            return context.Response.WriteAsync(json.ToString(Formatting.Indented));
        }
    }

}