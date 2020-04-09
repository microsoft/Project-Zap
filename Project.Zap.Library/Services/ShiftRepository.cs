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
            this.cosmosContainer = cosmosDatabase.CreateContainerIfNotExistsAsync("shifts", "/StoreName").Result;
        }
        public async Task Add(Shift item)
        {
            await this.cosmosContainer.CreateItemAsync<Shift>(item, new PartitionKey(item.StoreName));
        }

        public async Task Delete(Expression<Func<Shift, bool>> query)
        {
            IEnumerable<Shift> shifts = this.cosmosContainer.GetItemLinqQueryable<Shift>(true).Where(query);
            foreach (Shift item in shifts)
            {
                await this.cosmosContainer.DeleteItemAsync<Shift>(item.id, new PartitionKey(item.StoreName));
            }            
        }

        public Shift Get(string id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IEnumerable<Shift>> Get()
        {
            var query = this.cosmosContainer.GetItemQueryIterator<Shift>(new QueryDefinition("SELECT * FROM c")); 
            List<Shift> results = new List<Shift>(); 
            while (query.HasMoreResults) 
            { 
                var response = await query.ReadNextAsync(); 
                results.AddRange(response.ToList());
            }

            return results;            
        }

        public IEnumerable<Shift> Get(Expression<Func<Shift, bool>> query)
        {
            IEnumerable<Shift> storeShifts = this.cosmosContainer.GetItemLinqQueryable<Shift>(true).Where(query);

            return storeShifts;
        }

        public async Task<Shift> Update(Shift item)
        {
            Shift current = await this.cosmosContainer.ReadItemAsync<Shift>(item.id, new PartitionKey(item.StoreName));

            current.EmployeeId = item.EmployeeId;
            current.Allocated = item.Allocated;
            current.Start = item.Start;
            current.End = item.End;
            current.WorkType = item.WorkType;            

            return await this.cosmosContainer.ReplaceItemAsync<Shift>(current, current.id, new PartitionKey(current.StoreName));
        }


       
    }
}
