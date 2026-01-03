using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;
using MongoDBCore.Services;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly IMongoCollection<PolicyDto> _policiesCollection;
        private readonly ICacheService _cache;

        public PolicyRepository(IOptions<MongoDbSettings> options, ICacheService cache)
        {
            var settings = options.Value;
            var mongoClient = new MongoClient(settings.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);
            _policiesCollection = mongoDatabase.GetCollection<PolicyDto>(settings.PolicyCollectionName);
            _cache = cache;
        }
        public async Task DeleteAsync(int id)
        {
            var filter = Builders<PolicyDto>.Filter.Eq(x => x.PolicyId, id);
            await _policiesCollection.DeleteOneAsync(filter);
        }

        public async Task<List<PolicyDto>> GetAllAsync()
        {
            return await _policiesCollection.Find(_ => true).ToListAsync();
        }


        //Nếu bạn cache ở MongoDB: Bạn đang làm nhanh trải nghiệm cho tất cả người dùng cuối khi họ vào tra cứu hợp đồng.
        //Đây mới là giá trị thực sự của Microservices CQRS.
        public async Task<PolicyDto> GetByIdAsync(int id)
        {
            string cacheKey = $"policy:{id}";

            // Hàm GetOrSetAsync sẽ tự động thực hiện:
            // 1. Kiểm tra Redis (Get)
            // 2. Nếu trống -> Chạy hàm bên dưới để lấy từ MongoDB
            // 3. Lấy được dữ liệu -> Tự động Lưu vào Redis (Set)
            return await _cache.GetOrSetAsync(cacheKey, async () =>
            {
                return await _policiesCollection.Find(x => x.PolicyId == id).FirstOrDefaultAsync();
            }, TimeSpan.FromMinutes(30));
        }

        public async Task UpsertAsync(PolicyDto policytDto)
        {
            var filter = Builders<PolicyDto>.Filter.Eq(x => x.PolicyId , policytDto.PolicyId);
            await _policiesCollection.ReplaceOneAsync(filter, policytDto, new ReplaceOptions {  IsUpsert = true });
        }


        //Gia han hop dong va huy hop dong
        public async Task UpdateCancelStatusAsync(int id, string notes)
        {
            var filter = Builders<PolicyDto>.Filter.Eq(x => x.PolicyId, id);
            var update = Builders<PolicyDto>.Update
                .Set(x => x.Status, "CANCELLED");

            await _policiesCollection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateRenewStatusAsync(int id, DateTime newEndDate, decimal totalPremium, string notes)
        {
            var filter = Builders<PolicyDto>.Filter.Eq(x => x.PolicyId, id);
            var update = Builders<PolicyDto>.Update
                .Set(x => x.Status, "RENEWED")
                .Set(x => x.EndDate, newEndDate)
                .Set(x => x.PremiumAmount, totalPremium);
            await _policiesCollection.UpdateOneAsync(filter, update);
        }
    }
}
