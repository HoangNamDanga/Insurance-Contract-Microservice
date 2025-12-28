using MongoDBCore.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Services
{
    public  interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetUsers();
        Task<Customer?> GetUserById(string id);
        Task CreateUser(CustomerSyncDto user);
    }
}
