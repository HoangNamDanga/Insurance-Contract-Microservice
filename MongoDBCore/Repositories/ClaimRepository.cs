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
using System.Linq; // Đảm bảo có dòng này ở trên cùng file
namespace MongoDBCore.Repositories
{
    public class ClaimRepository : IClaimRepository
    {

        private readonly IMongoCollection<ClaimSyncDto> _claimsCollection;
        private readonly ICacheService _cache;
        private readonly IMongoCollection<PolicyDto> _policyCollection;
        public ClaimRepository(IOptions<MongoDbSettings> options, ICacheService cache)
        {
            var settings = options.Value;
            var mongoClient = new MongoClient(settings.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);

            _claimsCollection = mongoDatabase.GetCollection<ClaimSyncDto>(settings.ClaimsCollectionName);
            _policyCollection = mongoDatabase.GetCollection<PolicyDto>("Policy");

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
            await _claimsCollection.ReplaceOneAsync(filter, claimDoc, new ReplaceOptions { IsUpsert = true });

            try
            {
                await _cache.SetAsync($"claim:{claimDoc.ClaimId}", claimDoc, TimeSpan.FromMinutes(30));
            }
            catch (Exception ex)
            {
                // Chỉ log lỗi Redis, không làm dừng luồng chính vì DB đã lưu xong
                Console.WriteLine($"Lỗi cập nhật Cache: {ex.Message}");
            }
        }

        public async Task UpsertPolicySnapshotAsync(PolicyCreatedEvent dto)
        {
            if (dto == null) return;

            // 1. Filter: Dùng trực tiếp trường _id (vì bạn đã đánh dấu [BsonId] trong PolicyDto)
            var filter = Builders<PolicyDto>.Filter.Eq(x => x.PolicyId, dto.PolicyId);

            // 2. Mapping từ Event sang Dto (Snapshot hoàn chỉnh)
            var policyUpdate = new PolicyDto
            {
                PolicyId = dto.PolicyId,
                PolicyNumber = dto.PolicyNumber,
                CustomerId = dto.CustomerId,
                AgentId = dto.AgentId,
                InsTypeId = dto.InsTypeId,
                CustomerName = dto.CustomerName,
                AgentName = dto.AgentName,
                InsTypeName = dto.InsTypeName,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                PremiumAmount = dto.PremiumAmount,
                Status = dto.Status,

                // Map Object Vehicle (Tránh lỗi null nếu Oracle không gửi Vehicle)
                Vehicle = dto.Vehicle != null ? new PolicyDto.VehicleInfo
                {
                    Brand = dto.Vehicle.Brand,
                    Model = dto.Vehicle.Model
                } : new PolicyDto.VehicleInfo { Brand = "N/A", Model = "N/A" },

                // QUAN TRỌNG: Map danh sách Claims - Đảm bảo lấy đúng từ Event sang Dto
                Claims = dto.Claims?.Select(c => new PolicyDto.ClaimInfo
                {
                    ClaimId = c.ClaimId,
                    AmountApproved = c.AmountApproved,
                    Status = c.Status
                }).ToList() ?? new List<PolicyDto.ClaimInfo>()
            };

            // 3. Thực hiện ReplaceOne với IsUpsert = true
            // Lệnh này sẽ tìm bản ghi có _id = 41, xóa sạch nội dung cũ và đập nguyên cục policyUpdate vào
            await _policyCollection.ReplaceOneAsync(
                filter,
                policyUpdate,
                new ReplaceOptions { IsUpsert = true }
            );
        }
    }
}
