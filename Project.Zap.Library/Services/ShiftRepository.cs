using Microsoft.Azure.Cosmos;
using Project.Zap.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public class ShiftRepository : IRepository<Shift>
    {
        private readonly Container cosmosContainer;

        public ShiftRepository(Database cosmosDatabase)
        {
            this.cosmosContainer = cosmosDatabase.CreateContainerIfNotExistsAsync("shifts", "/LocationId").Result;
        }
        public async Task Add(Shift item)
        {
            await this.cosmosContainer.CreateItemAsync<Shift>(item, new PartitionKey(item.LocationId));
        }

        public async Task Delete(Expression<Func<Shift, bool>> query)
        {
            IEnumerable<Shift> shifts = this.cosmosContainer.GetItemLinqQueryable<Shift>(true).Where(query);
            foreach (Shift item in shifts)
            {
                await this.cosmosContainer.DeleteItemAsync<Shift>(item.id, new PartitionKey(item.LocationId));
            }            
        }

        public async Task<IEnumerable<Shift>> Get()
        {
            return await this.Get("SELECT * FROM c");
        }

        public async Task<IEnumerable<Shift>> Get(string sql, IDictionary<string, object> parameters = null, string partitionKey = null)
        {
            QueryDefinition query = new QueryDefinition(sql);

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    query.WithParameter(parameter.Key, parameter.Value);
                }
            }

            QueryRequestOptions options = new QueryRequestOptions() { MaxBufferedItemCount = 100, MaxConcurrency = 10 };
            if (partitionKey != null) options.PartitionKey = new PartitionKey(partitionKey);

            List<Shift> results = new List<Shift>();
            FeedIterator<Shift> iterator = this.cosmosContainer.GetItemQueryIterator<Shift>(query, requestOptions: options);
            while (iterator.HasMoreResults)
            {
                FeedResponse<Shift> response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public async Task<Shift> Update(Shift item)
        {
            Shift current = await this.cosmosContainer.ReadItemAsync<Shift>(item.id, new PartitionKey(item.LocationId));

            current.EmployeeId = item.EmployeeId;
            current.Allocated = item.Allocated;
            current.Start = item.Start;
            current.End = item.End;
            current.WorkType = item.WorkType;            

            return await this.cosmosContainer.ReplaceItemAsync<Shift>(current, current.id, new PartitionKey(current.LocationId));
        }       
    }
}
