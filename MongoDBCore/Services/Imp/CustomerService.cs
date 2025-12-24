using MongoDBCore.Entities.Models;
using MongoDBCore.Entities.Models.DTOs;
using MongoDBCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Services.Imp
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        // DI Repository vào Service
        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }
        // Triển khai logic, gọi Repository
        public Task<IEnumerable<Customer>> GetUsers() => _customerRepository.GetAllAsync();
        public Task<Customer?> GetUserById(string id) => _customerRepository.GetByIdAsync(id);
        public Task CreateUser(CustomerSyncDto user) => _customerRepository.CreateAsync(user);
    }
}
