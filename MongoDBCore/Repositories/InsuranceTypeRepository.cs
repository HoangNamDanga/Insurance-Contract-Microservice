using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories
{
    public class InsuranceTypeRepository : IInsuranceRepository
    {
        private readonly IMongoCollection<InsuranceTypeDto> _insuranceCollection;

        // Constructor: Nhận các thiết lập kết nối (MongoDBSettings)
        public InsuranceTypeRepository(IOptions<MongoDbSettings> options)
        {
            var settings = options.Value; // Lấy giá trị thực từ options
            // Tạo MongoClient để kết nối tới MongoDB
            var mongoClient = new MongoClient(settings.ConnectionString);  // Chú ý: Đảm bảo ConnectionString là kiểu string

            // Lấy Database
            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);

            // Lấy Collection để thao tác
            _insuranceCollection = mongoDatabase.GetCollection<InsuranceTypeDto>(settings.InsuranceTypeCollectionName);
        }
        // Hàm lấy danh sách (Read)
        public async Task<List<InsuranceTypeDto>> GetAllAsync()
        {
            // Find(_ => true) nghĩa là lấy tất cả không điều kiện
            return await _insuranceCollection.Find(_ => true).ToListAsync();
        }

        // Hàm lấy theo ID (Read)
        public async Task<InsuranceTypeDto> GetByIdAsync(int id)
        {
            return await _insuranceCollection.Find(x => x.InsTypeId == id).FirstOrDefaultAsync();
        }

        public async Task UpsertAsync(InsuranceTypeDto dto)
        {
            var filter = Builders<InsuranceTypeDto>.Filter.Eq(x => x.InsTypeId, dto.InsTypeId);
            // IsUpsert = true: Tự động tạo Collection/Document nếu chưa tồn tại
            await _insuranceCollection.ReplaceOneAsync(filter, dto, new ReplaceOptions { IsUpsert = true });
        }

        public async Task DeleteAsync(int id)
        {
            var filter = Builders<InsuranceTypeDto>.Filter.Eq(x => x.InsTypeId, id);
            await _insuranceCollection.DeleteOneAsync(filter);
        }
    }
}
