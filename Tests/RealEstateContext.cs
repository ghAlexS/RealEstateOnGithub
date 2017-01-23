namespace Tests
{
    using MongoDB.Driver;
    using RealEstate.Properties;
    using RealEstate.Rentals;

    public class RealEstateContext
    {
        public MongoDatabase Database;

        public RealEstateContext()
        {
            var client = new MongoClient(Settings.Default.RealEstateConnectionString);
            var server = client.GetServer();
            Database = server.GetDatabase(Settings.Default.RealEstateDatabaseName);
        }

        public MongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }
}