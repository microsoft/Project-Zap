using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(Project.Zap.Shifts.ChangeFeed.Startup))]

namespace Project.Zap.Shifts.ChangeFeed
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            string bingMapsKey = System.Environment.GetEnvironmentVariable("BingMapsKey");
            HttpClient client = new HttpClient
            {
                BaseAddress = new System.Uri($"http://dev.virtualearth.net/REST/v1/Locations?key={bingMapsKey}&culture=en-GB")
            };

            builder.Services.AddSingleton<HttpClient>(client);
            builder.Services.AddSingleton<Container>(this.GetCosmosContainer().Result);

            string searchServiceName = System.Environment.GetEnvironmentVariable("SearchServiceName");
            string adminApiKey = System.Environment.GetEnvironmentVariable("SearchServiceQueryApiKey");

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));

            Index index = serviceClient.Indexes.CreateOrUpdate(new Index { Name = "shifts", Fields = FieldBuilder.BuildForType<ShiftSearchDocument>() });

            builder.Services.AddSingleton<ISearchIndexClient>(x => serviceClient.Indexes.GetClient(index.Name));
        }

        private async Task<Container> GetCosmosContainer()
        {
            string connectionString = System.Environment.GetEnvironmentVariable("CosmosConnectionString");
            CosmosClient client = new CosmosClientBuilder(connectionString).Build();
            Database database = await client.CreateDatabaseIfNotExistsAsync("zap");
            return await database.CreateContainerIfNotExistsAsync("locations", "/Name");
        }
    }
}
