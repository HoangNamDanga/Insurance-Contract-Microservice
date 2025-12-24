using OracleSQLCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetAllCustomers();
        Task<Customer> GetCustomerDetails(int customerId);
        Task<int> CreateCustomer(Customer customer);
        Task<bool> UpdateCustomerInfo(Customer customer);
        Task<bool> RemoveCustomer(int customerId);
    }
}
