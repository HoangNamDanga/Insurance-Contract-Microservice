using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services.Imp
{
    public  class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<int> CreateCustomer(Customer customer)
        {
            // Thêm Logic nghiệp vụ: Ví dụ, kiểm tra email không trùng lặp
            if (string.IsNullOrEmpty(customer.Email))
            {
                throw new ArgumentException("Email is required.");
            }
            return await _customerRepository.AddCustomerAsync(customer);
        }

        public async Task<bool> RemoveCustomer(int customerId)
        {
            // Thêm Logic nghiệp vụ: Kiểm tra điều kiện xóa (ví dụ: khách hàng phải không có đơn hàng đang mở)
            return await _customerRepository.DeleteCustomerAsync(customerId);
        }

        public Task<IEnumerable<Customer>> GetAllCustomers() => _customerRepository.GetAllCustomersAsync();
        public Task<Customer> GetCustomerDetails(int customerId) => _customerRepository.GetCustomerByIdAsync(customerId);
        public Task<bool> UpdateCustomerInfo(Customer customer) => _customerRepository.UpdateCustomerAsync(customer);
    }
}
