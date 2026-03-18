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
    public class VehicleRepository : IVehicleRepository
    {
        private readonly IMongoCollection<VehicleDto> _collection;
        private readonly ICacheService _cache;

        public VehicleRepository(
            IMongoClient mongoClient,
            IOptions<MongoDbSettings> settings,
            ICacheService cache) // Inject ICacheService vào đây
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<VehicleDto>("Vehicles");
            _cache = cache;
        }

        public async Task<VehicleDto> GetVehicleByIdAsync(int policyId, int vehicleId)
        {
            string cacheKey = $"mongo_vehicle_{vehicleId}";

            var cached = await _cache.GetAsync<VehicleDto>(cacheKey);
            if (cached != null) return cached;

            var vehicle = await _collection.Find(v => v.VehicleId == vehicleId && v.PolicyId == policyId)
                                          .FirstOrDefaultAsync();

            if (vehicle != null)
            {
                await _cache.SetAsync(cacheKey, vehicle, TimeSpan.FromMinutes(30));
            }

            return vehicle;
        }

        //Bởi vì update sang xóa cache . nên khi người dùng gọi Get nào dữ liệu cũng là mới nhất
        public async Task<IEnumerable<VehicleDto>> GetVehiclesByPolicyIdAsync(int policyId)
        {
            string cacheKey = $"mongo_vehicles_policy_{policyId}";

            var cachedList = await _cache.GetAsync<IEnumerable<VehicleDto>>(cacheKey);
            if (cachedList != null) return cachedList;

            var list = await _collection.Find(v => v.PolicyId == policyId).ToListAsync();

            if (list != null && list.Any())
            {
                await _cache.SetAsync(cacheKey, list, TimeSpan.FromMinutes(30));
            }

            return list;
        }

        public async Task<bool> RemoveVehicleAsync(int policyId, int vehicleId)
        {
            var filter = Builders<VehicleDto>.Filter.And(
                            Builders<VehicleDto>.Filter.Eq(v => v.VehicleId, vehicleId),
                            Builders<VehicleDto>.Filter.Eq(v => v.PolicyId, policyId)
                        );

            var result = await _collection.DeleteOneAsync(filter);

            if (result.DeletedCount > 0)
            {
                // Dọn dẹp cache
                await _cache.RemoveAsync($"mongo_vehicles_policy_{policyId}");
                await _cache.RemoveAsync($"mongo_vehicle_{vehicleId}");
                return true;
            }
            return false;
        }

        public async Task<bool> UpsertVehicleAsync(int policyId, VehicleCreatedEvent vehicleData)
        {
            // Filter tìm xe dựa trên VehicleId (từ Oracle)
            var filter = Builders<VehicleDto>.Filter.Eq(v => v.VehicleId, vehicleData.VehicleId);

            var vehicleDto = new VehicleDto
            {
                VehicleId = vehicleData.VehicleId,
                PolicyId = policyId,
                LicensePlate = vehicleData.LicensePlate,
                Brand = vehicleData.Brand,
                Model = vehicleData.Model,
                YearManufactured = vehicleData.YearManufactured,
                CreatedAt = vehicleData.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            // IsUpsert = true: Tự động thêm mới nếu không tìm thấy ID
            var result = await _collection.ReplaceOneAsync(filter, vehicleDto, new ReplaceOptions { IsUpsert = true });

            if (result.IsAcknowledged)
            {
                // Xóa cache danh sách của Policy vì dữ liệu đã thay đổi
                await _cache.RemoveAsync($"mongo_vehicles_policy_{policyId}");
                await _cache.RemoveAsync($"mongo_vehicle_{vehicleData.VehicleId}");
                return true;
            }
            return false;
        }
    }
}
