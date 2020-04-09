using Microsoft.Azure.Cosmos;
using Project.Zap.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public class EmployeeRepository : IRepository<Employee>
    {
        private readonly Container cosmosContainer;

        public EmployeeRepository(Database cosmosDatabase)
        {
            this.cosmosContainer = cosmosDatabase.CreateContainerIfNotExistsAsync("employees", "/EmployeeId").Result;
        }

        public async Task Add(Employee item)
        {
            await this.cosmosContainer.CreateItemAsync<Employee>(item);
        }

        public Task Delete(Expression<Func<Employee, bool>> query)
        {
            throw new NotImplementedException();
        }

        public Employee Get(string id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Employee> Get(Expression<Func<Employee, bool>> query)
        {
            return this.cosmosContainer.GetItemLinqQueryable<Employee>(true).Where(query).AsEnumerable();
        }

        public Task<IEnumerable<Employee>> Get()
        {
            throw new NotImplementedException();
        }

        public Task<Employee> Update(Employee item)
        {
            throw new NotImplementedException();
        }
    }
}
