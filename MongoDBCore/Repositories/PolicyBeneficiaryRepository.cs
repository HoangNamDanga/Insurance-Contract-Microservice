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
    public class PolicyBeneficiaryRepository : IPolicyBeneficiaryRepository
    {
        private readonly IMongoCollection<PolicyBeneficiaryDto> _collection;
        private readonly ICacheService _cache;

        public PolicyBeneficiaryRepository(
            IMongoClient mongoClient,
            IOptions<MongoDbSettings> settings,
            ICacheService cache) // Inject ICacheService vào đây
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<PolicyBeneficiaryDto>("PolicyBeneficiaries");
            _cache = cache;
        }

        public async Task<PolicyBeneficiaryDto> GetPolicyBeneficiaryByIdAsync(int policyId, int beneficiaryeId)
        {
            string cacheKey = $"mongo_beneficiary_{beneficiaryeId}";

            var cached = await _cache.GetAsync<PolicyBeneficiaryDto>(cacheKey);
            if (cached != null) return cached;

            var beneficiarye = await _collection.Find(v => v.BeneficiaryId == beneficiaryeId && v.PolicyId == policyId)
                                          .FirstOrDefaultAsync();

            if (beneficiarye != null)
            {
                await _cache.SetAsync(cacheKey, beneficiarye, TimeSpan.FromMinutes(30));
            }

            return beneficiarye;
        }

        public async Task<IEnumerable<PolicyBeneficiaryDto>> GetPolicyBeneficiaryByPolicyIdAsync(int policyId)
        {
            string cacheKey = $"mongo_beneficiarye_policy_{policyId}";

            var cachedList = await _cache.GetAsync<IEnumerable<PolicyBeneficiaryDto>>(cacheKey);
            if (cachedList != null) return cachedList;

            var list = await _collection.Find(v => v.PolicyId == policyId).ToListAsync();

            if (list != null && list.Any())
            {
                await _cache.SetAsync(cacheKey, list, TimeSpan.FromMinutes(30));
            }

            return list;
        }

        public async Task<bool> RemovePolicyBeneficiaryAsync(int policyId, int beneficiaryeId)
        {
            var filter = Builders<PolicyBeneficiaryDto>.Filter.And(
                Builders<PolicyBeneficiaryDto>.Filter.Eq(v => v.BeneficiaryId, beneficiaryeId),
                Builders<PolicyBeneficiaryDto>.Filter.Eq(v => v.PolicyId, policyId)
            );

            var result = await _collection.DeleteOneAsync(filter);

            if (result.DeletedCount > 0)
            {
                // Dọn dẹp cache
                await _cache.RemoveAsync($"mongo_vehicles_policy_{policyId}");
                await _cache.RemoveAsync($"mongo_vehicle_{beneficiaryeId}");
                return true;
            }
            return false;
        }

        public async Task<bool> UpsertPolicyBeneficiaryAsync(int policyId, PolicyBeneficiaryCreatedEvent policyBeneficiaryeData)
        {
            var filter = Builders<PolicyBeneficiaryDto>.Filter.Eq(v => v.BeneficiaryId, policyBeneficiaryeData.BeneficiaryId);

            var vehicleDto = new PolicyBeneficiaryDto
            {
                BeneficiaryId = policyBeneficiaryeData.BeneficiaryId,
                PolicyId = policyId,
                Relationship = policyBeneficiaryeData.Relationship,
                FullName = policyBeneficiaryeData.FullName,
                Phone = policyBeneficiaryeData.Phone,
                CreatedAt = policyBeneficiaryeData.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            // IsUpsert = true: Tự động thêm mới nếu không tìm thấy ID
            var result = await _collection.ReplaceOneAsync(filter, vehicleDto, new ReplaceOptions { IsUpsert = true });

            if (result.IsAcknowledged)
            {
                // Xóa cache danh sách của Policy vì dữ liệu đã thay đổi
                await _cache.RemoveAsync($"mongo_beneficiarye_policy_{policyId}");
                await _cache.RemoveAsync($"mongo_beneficiary_{policyBeneficiaryeData.BeneficiaryId}");
                return true;
            }
            return false;
        }
    }
}
