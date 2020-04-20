using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Spatial;
using Project.Zap.Library.Models;
using Document = Microsoft.Azure.Documents.Document;

namespace Project.Zap.Shifts.ChangeFeed
{
    public class SearchShiftDocumentBuilder
    {
        private readonly Container container;
        private readonly HttpClient client;
        private readonly ISearchIndexClient searchIndexClient;
        private readonly IDictionary<string, GeographyPoint> points;
        private readonly IDictionary<string, Location> locations;

        public SearchShiftDocumentBuilder(Container container, HttpClient client, ISearchIndexClient searchIndexClient)
        {
            this.container = container;
            this.client = client;
            this.searchIndexClient = searchIndexClient;
            this.points = new Dictionary<string, GeographyPoint>();
            this.locations = new Dictionary<string, Location>();
        }

        [FunctionName("SearchShiftDocumentBuilder")]
        public async Task Run([CosmosDBTrigger(
            databaseName: "zap",
            collectionName: "shifts",
            ConnectionStringSetting = "CosmosConnectionString",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<Document> input, ILogger logger)
        {
            if (input != null && input.Count > 0)
            {
                logger.LogInformation($"Documents modified {input.Count}");
            }

            List<ShiftSearchDocument> documents = new List<ShiftSearchDocument>();

            foreach (Document document in input)
            {
                logger.LogInformation($"Processing Document: {document.Id}");
                Shift shift = JsonSerializer.Deserialize<Shift>(document.ToString());
                Location location = this.GetLocation(shift.LocationId);                

                ShiftSearchDocument shiftSearchDocument = new ShiftSearchDocument
                {
                    Id = $"{location.id}_{shift.id}",
                    LocationName = location.Name,
                    City = location.Address.City,
                    StartDateTime = shift.StartDateTime,
                    EndDateTime = shift.EndDateTime,
                    WorkType = shift.WorkType,
                    Location = await this.GetGeographyPoint(location.Address.ZipOrPostCode)
                };

                documents.Add(shiftSearchDocument);
            }

            IndexBatch<ShiftSearchDocument> batch = IndexBatch.Upload(documents);

            this.searchIndexClient.Documents.Index(batch);
        }

        private Location GetLocation(string id)
        {
            if(!this.locations.ContainsKey(id))
            {
                Location location = this.container.GetItemLinqQueryable<Location>(true).Where(x => x.id == id).AsEnumerable().FirstOrDefault();
                this.locations.TryAdd(id, location);
            }

            return this.locations[id];
        }

        private async Task<GeographyPoint> GetGeographyPoint(string zipOrPostcode)
        {
            if(!this.points.ContainsKey(zipOrPostcode))
            {
                HttpResponseMessage httpResponse = await this.client.GetAsync($"{this.client.BaseAddress}&PostalCode={zipOrPostcode}");
                BingMapResponse bingResponse = await httpResponse.Content.ReadAsAsync<BingMapResponse>();
                this.points.TryAdd(zipOrPostcode, GeographyPoint.Create(bingResponse.resourceSets[0].resources[0].point.coordinates[0], bingResponse.resourceSets[0].resources[0].point.coordinates[1]));
            }

            return this.points[zipOrPostcode];
        }
    }
}
