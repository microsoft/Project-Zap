using Microsoft.Azure.Cosmos;
using Project.Zap.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public class PartnerRepository : IRepository<PartnerOrganization>
    {
        private readonly Container cosmosContainer;

        public PartnerRepository(Database cosmosDatabase)
        {
            this.cosmosContainer = cosmosDatabase.CreateContainerIfNotExistsAsync("partner", "/Name").Result;
        }

        public async Task Add(PartnerOrganization item)
        {
            await this.cosmosContainer.CreateItemAsync<PartnerOrganization>(item, new PartitionKey(item.Name));
        }

        public async Task Delete(Expression<Func<PartnerOrganization, bool>> query)
        {
            IEnumerable<dynamic> results = this.cosmosContainer.GetItemLinqQueryable<PartnerOrganization>(true).Where(query).Select(x => new { id = x.id, Name = x.Name }).AsEnumerable();
            foreach(dynamic result in results)
            {
                await this.cosmosContainer.DeleteItemAsync<PartnerOrganization>(result.id, new PartitionKey(result.Name));
            }
        }

        public async Task<IEnumerable<PartnerOrganization>> Get()
        {
            return await this.Get("SELECT * FROM c");
        }

        public async Task<IEnumerable<PartnerOrganization>> Get(string sql, IDictionary<string, object> parameters = null, string partitionKey = null)
        {
            QueryDefinition query = new QueryDefinition(sql);

            foreach (var parameter in parameters)
            {
                query.WithParameter(parameter.Key, parameter.Value);
            }

            QueryRequestOptions options = new QueryRequestOptions() { MaxBufferedItemCount = 100, MaxConcurrency = 10 };
            if (partitionKey != null) options.PartitionKey = new PartitionKey(partitionKey);

            List<PartnerOrganization> results = new List<PartnerOrganization>();
            FeedIterator<PartnerOrganization> iterator = this.cosmosContainer.GetItemQueryIterator<PartnerOrganization>(query, requestOptions: options);
            while (iterator.HasMoreResults)
            {
                FeedResponse<PartnerOrganization> response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public Task<PartnerOrganization> Update(PartnerOrganization item)
        {
            throw new NotImplementedException();
        }
    }
}
