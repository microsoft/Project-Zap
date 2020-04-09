using Microsoft.Azure.Cosmos;
using Project.Zap.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public class OrganizationRepository : IRepository<Organization>
    {
        private Container cosmosContainer;
        public OrganizationRepository(Database cosmosDatabase)
        {
            this.cosmosContainer = cosmosDatabase.CreateContainerIfNotExistsAsync("organization", "/Name").Result;
        }

        public async Task Add(Organization item)
        {
            await this.cosmosContainer.CreateItemAsync(item, new PartitionKey(item.Name));
        }

        public Task Delete(Expression<Func<Organization, bool>> query)
        {
            throw new NotImplementedException();
        }

        public Organization Get(string id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<Organization>> Get()
        {
            return Task.FromResult(this.cosmosContainer.GetItemLinqQueryable<Organization>(true).Where(x => x.StoreType == StoreTypes.Open).AsEnumerable());
        }

        public IEnumerable<Organization> Get(Expression<Func<Organization, bool>> query)
        {
            throw new NotImplementedException();
        }

        public async Task<Organization> Update(Organization item)
        {
            ItemResponse<Organization> current = await this.cosmosContainer.ReadItemAsync<Organization>(item.id, new PartitionKey(item.Name));

            current.Resource.Stores = new List<Store>();

            foreach(Store store in item.Stores)
            {
                current.Resource.Stores.Add(store);
            }

            return await this.cosmosContainer.ReplaceItemAsync<Organization>(current.Resource, current.Resource.id, new PartitionKey(current.Resource.Name));
        }
    }
}
