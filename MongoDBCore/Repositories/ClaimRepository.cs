using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;
using MongoDBCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories
{
    public class ClaimRepository : IClaimRepository
    {

        private readonly IMongoCollection<ClaimSyncDto> _claimsCollection;
        private readonly ICacheService _cache;
        public ClaimRepository(IOptions<MongoDbSettings> options, ICacheService cache)
        {
            var settings = options.Value;
            var mongoClient = new MongoClient(settings.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);

            _claimsCollection = mongoDatabase.GetCollection<ClaimSyncDto>(settings.ClaimsCollectionName);

            _cache = cache;
        }

        public async Task<ClaimSyncDto> GetByIdAsync(int claimId)
        {
            string cacheKey = $"claim:{claimId}";

            // Sử dụng Redis để tối ưu tốc độ phản hồi
            return await _cache.GetOrSetAsync(cacheKey, async () =>
            {
                // Nếu Redis không có, tìm trong MongoDB
                return await _claimsCollection
                    .Find(x => x.ClaimId == claimId)
                    .FirstOrDefaultAsync();
            }, TimeSpan.FromMinutes(30)); // Cache trong 30 phút
        }

        //phục vụ tốc độ và trải nghiệm người dùng.
        public async Task<IEnumerable<ClaimSyncDto>> GetClaimsByCustomerAsync(string customerName)
        {
            return await _claimsCollection.Find(x => x.CustomerName == customerName).ToListAsync();
        }

        // Đừng quên cập nhật hàm Upsert để xóa Cache cũ khi dữ liệu thay đổi!
        public async Task UpsertClaimAsync(ClaimSyncDto claimDoc)
        {
            var filter = Builders<ClaimSyncDto>.Filter.Eq(x => x.ClaimId, claimDoc.ClaimId);

            // 1. Lưu vào MongoDB
            await _claimsCollection.ReplaceOneAsync(filter, claimDoc, new ReplaceOptions { IsUpsert = true });

            // 2. XÓA CACHE CŨ (Rất quan trọng)
            // Nếu không xóa, người dùng sẽ thấy dữ liệu cũ từ Redis thay vì dữ liệu mới vừa cập nhật
            await _cache.RemoveAsync($"claim:{claimDoc.ClaimId}");
        }
    }
}
