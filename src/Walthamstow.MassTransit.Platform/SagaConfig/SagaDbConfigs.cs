using System.Collections.Generic;

namespace Walthamstow.MassTransit.Platform.SagaConfig
{
    public class SagaDbConfigs
    {
        public List<MongoDbConfigOptions> SagaMongoDbOptions { get; set; }
    }
}