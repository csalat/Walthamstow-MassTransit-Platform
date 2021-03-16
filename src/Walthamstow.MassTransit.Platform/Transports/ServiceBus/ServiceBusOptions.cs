namespace Walthamstow.MassTransit.Platform.Transports.ServiceBus
{
    public class ServiceBusOptions
    {
        public string ConnectionString { get; set; }
        public bool Enabled { get; set; }
    }

}