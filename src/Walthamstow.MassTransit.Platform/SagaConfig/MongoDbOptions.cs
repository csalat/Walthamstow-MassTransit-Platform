namespace Walthamstow.MassTransit.Platform.SagaConfig
{
    public class MongoDbConfigOptions
    {
        
        /// <summary>
        /// Sets the Saga name
        /// </summary>
        public string SagaName{ get; set; }
        
        /// <summary>
        /// Sets the database factory using connection string <see cref="T:MongoDB.Driver.MongoUrl" />
        /// </summary>
        public string Connection { get; set; }

        /// <summary>Sets the database name</summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Sets the collection name using <see cref="T:MassTransit.MongoDbIntegration.Saga.CollectionNameFormatters.DefaultCollectionNameFormatter" />
        /// </summary>
        public string CollectionName { get; set; }
    }
}