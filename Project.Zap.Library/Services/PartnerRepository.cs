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

        public PartnerOrganization Get(string id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PartnerOrganization> Get(Expression<Func<PartnerOrganization, bool>> query)
        {
            IEnumerable<PartnerOrganization> partners = this.cosmosContainer.GetItemLinqQueryable<PartnerOrganization>(true).Where(query);
            return partners;
        }

        public Task<IEnumerable<PartnerOrganization>> Get()
        {
            return Task.FromResult(this.cosmosContainer.GetItemLinqQueryable<PartnerOrganization>(true).AsEnumerable());
        }

        public Task<PartnerOrganization> Update(PartnerOrganization item)
        {
            throw new NotImplementedException();
        }
    }
}
