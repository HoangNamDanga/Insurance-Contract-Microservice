using MongoDBCore.Entities.Models;
using MongoDBCore.Entities.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Interfaces
{
    public  interface ICustomerRepository
    {
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(string id);
        Task CreateAsync(CustomerSyncDto newCustomer);
        Task UpdateAsync(string id, Customer updatedCustomer);
        Task RemoveAsync(string id);
    }
}
