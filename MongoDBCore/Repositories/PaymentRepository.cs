using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;
using MongoDBCore.Services; // Đảm bảo có namespace này cho ICacheService
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly IMongoCollection<PaymentDto> _collection;
        private readonly ICacheService _cache;

        public PaymentRepository(
            IMongoClient mongoClient,
            IOptions<MongoDbSettings> settings,
            ICacheService cache) // Inject ICacheService vào đây
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<PaymentDto>("Payments");
            _cache = cache;
        }

        public async Task UpsertPaymentAsync(PaymentDto payment)
        {
            var filter = Builders<PaymentDto>.Filter.Eq(p => p.PaymentId, payment.PaymentId);

            // 1. Cập nhật vào MongoDB
            await _collection.ReplaceOneAsync(filter, payment, new ReplaceOptions { IsUpsert = true });

            // 2. Cập nhật đồng thời vào Redis để dữ liệu luôn mới nhất
            try
            {
                string cacheKey = $"paymentmongo:{payment.PaymentId}";
                await _cache.SetAsync(cacheKey, payment, TimeSpan.FromMinutes(30));
            }
            catch (Exception ex)
            {
                // Log lỗi Redis nhưng không chặn luồng chính
                Console.WriteLine($"[Redis Error] Không thể cập nhật cache cho Payment {payment.PaymentId}: {ex.Message}");
            }
        }

        public async Task<PaymentDto?> GetByIdAsync(decimal paymentId)
        {
            string cacheKey = $"paymentmongo:{paymentId}";

            // Sử dụng cơ chế GetOrSet: Nếu Redis có thì lấy luôn, không có thì vào Mongo tìm rồi lưu lại Redis
            return await _cache.GetOrSetAsync(cacheKey, async () =>
            {
                return await _collection.Find(p => p.PaymentId == paymentId).FirstOrDefaultAsync();
            }, TimeSpan.FromMinutes(30));
        }

        public async Task<IEnumerable<PaymentDto>> GetByPolicyIdAsync(decimal policyId)
        {
            // Với danh sách (History), thường ta sẽ truy vấn trực tiếp từ MongoDB 
            // vì danh sách hay thay đổi và phức tạp hơn để quản lý cache key.
            return await _collection.Find(p => p.PolicyId == policyId).ToListAsync();
        }
    }
}