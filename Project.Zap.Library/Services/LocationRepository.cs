using Microsoft.Azure.Cosmos;
using Project.Zap.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public class LocationRepository : IRepository<Location>
    {
        private Container cosmosContainer;
        public LocationRepository(Database cosmosDatabase)
        {
            this.cosmosContainer = cosmosDatabase.CreateContainerIfNotExistsAsync("locations", "/Name").Result;
        }

        public async Task Add(Location item)
        {
            await this.cosmosContainer.CreateItemAsync(item, new PartitionKey(item.Name));
        }

        public async Task Delete(Expression<Func<Location, bool>> query)
        {
            Location location = this.cosmosContainer.GetItemLinqQueryable<Location>(true).Where(query).AsEnumerable().FirstOrDefault();
            if (location is null) return;

            await this.cosmosContainer.DeleteItemAsync<Location>(location.id, new PartitionKey(location.Name));
        }
        public async Task<IEnumerable<Location>> Get()
        {
            return await this.Get("SELECT * FROM c");
        }

        public async Task<IEnumerable<Location>> Get(string sql, IDictionary<string, object> parameters = null, string partitionKey = null)
        {
            QueryDefinition query = new QueryDefinition(sql);

            if(parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    query.WithParameter(parameter.Key, parameter.Value);
                }
            }

            QueryRequestOptions options = new QueryRequestOptions() { MaxBufferedItemCount = 100, MaxConcurrency = 10 };
            if (partitionKey != null) options.PartitionKey = new PartitionKey(partitionKey);

            List<Location> results = new List<Location>();
            FeedIterator<Location> iterator = this.cosmosContainer.GetItemQueryIterator<Location>(query, requestOptions: options);
            while (iterator.HasMoreResults)
            {
                FeedResponse<Location> response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public async Task<Location> Replace(Location item)
        {
            return await this.cosmosContainer.ReplaceItemAsync<Location>(item, item.id, new PartitionKey(item.Name));
        }

        public Task<Location> Update(Location item)
        {
            throw new NotImplementedException();            
        }
    }
}
