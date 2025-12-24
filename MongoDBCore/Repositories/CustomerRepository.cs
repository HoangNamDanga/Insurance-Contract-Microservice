using MongoDB.Driver;
using MongoDBCore.Entities.Models;
using MongoDBCore.Entities.Models.DTOs;
using MongoDBCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {

        private readonly IMongoCollection<Customer> _customerCollection;

        // Constructor: Nhận các thiết lập kết nối (MongoDBSettings)
        public CustomerRepository(MongoDbSettings settings)
        {
            // Tạo MongoClient để kết nối tới MongoDB
            var mongoClient = new MongoClient(settings.ConnectionString);  // Chú ý: Đảm bảo ConnectionString là kiểu string

            // Lấy Database
            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);

            // Lấy Collection để thao tác
            _customerCollection = mongoDatabase.GetCollection<Customer>(settings.CustomerCollectionName);
        }


        public async Task<IEnumerable<Customer>> GetAllAsync() =>
                    await _customerCollection.Find(_ => true).ToListAsync();

        public async Task<Customer?> GetByIdAsync(string id) =>
            await _customerCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(CustomerSyncDto dto)
        {
            var mongoCustomer = new Customer
            {
                Id = dto.CustomerId,
                FullName = dto.FullName,
                Email = dto.Email
            };
            await _customerCollection.InsertOneAsync(mongoCustomer);
        }


        public async Task UpdateAsync(string id, Customer updatedUser) =>
            await _customerCollection.ReplaceOneAsync(x => x.Id == id, updatedUser);

        public async Task RemoveAsync(string id) =>
            await _customerCollection.DeleteOneAsync(x => x.Id == id);
    }
}
