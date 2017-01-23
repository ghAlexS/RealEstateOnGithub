namespace RealEstate.App_Start
{
    using MongoDB.Driver;
    using MongoDB.Driver.GridFS;
    using Properties;
    using Rentals;

    public class RealEstateContextNewApis
    {
        public IMongoDatabase Database;

        public RealEstateContextNewApis()
        {
            var connectionString = Settings.Default.RealEstateConnectionString;
            var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            settings.ClusterConfigurator = builder => builder.Subscribe(new Log4NetMongoEvents());
            var client = new MongoClient(settings);
            Database = client.GetDatabase(Settings.Default.RealEstateDatabaseName);
            ImagesBucket = new GridFSBucket(Database);
        }

        public GridFSBucket ImagesBucket { get; set; }

        public IMongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }
}