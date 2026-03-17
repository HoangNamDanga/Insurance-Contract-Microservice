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
    public class ClaimDocumentRepository : IClaimDocumentRepository
    {
        private readonly IMongoCollection<ClaimDocumentMongo> _claimDocumentsCollection;
        private readonly ICacheService _cache;

        public ClaimDocumentRepository(IOptions<MongoDbSettings> options)
        {
            var settings = options.Value;
            var mongoClient = new MongoClient(settings.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);

            _claimDocumentsCollection = mongoDatabase.GetCollection<ClaimDocumentMongo>(settings.ClaimDocumentsCollectionName);
        }

        // 1. Lấy tài liệu theo ID (ID từ Oracle đồng bộ sang)
        public async Task<ClaimDocumentMongo?> GetByIdAsync(int docId)
        {
            string cacheKey = $"claim_doc:{docId}";

            return await _cache.GetOrSetAsync(cacheKey, async () =>
            {
                // Nếu Redis trống, tìm trong MongoDB
                return await _claimDocumentsCollection
                    .Find(d => d.DocId == docId)
                    .FirstOrDefaultAsync();
            }, TimeSpan.FromMinutes(30));
        }

        // 2. Lấy danh sách tài liệu theo ClaimId (Dùng cho Read Model)
        public async Task<IEnumerable<ClaimDocumentMongo>> GetByClaimIdAsync(int claimId)
        {
            // Với danh sách, thường ta lấy trực tiếp từ Mongo để đảm bảo data mới nhất
            return await _claimDocumentsCollection
                .Find(d => d.ClaimId == claimId)
                .ToListAsync();
        }

        // 3. Hàm Upsert (Cực kỳ quan trọng để đồng bộ)
        public async Task UpsertAsync(ClaimDocumentMongo document)
        {
            var filter = Builders<ClaimDocumentMongo>.Filter.Eq(d => d.DocId, document.DocId);

            // Cập nhật vào MongoDB
            await _claimDocumentsCollection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = true });

            // Cập nhật lại Cache ngay lập tức để người dùng thấy data mới nhất
            try
            {
                string cacheKey = $"claim_doc:{document.DocId}";
                await _cache.SetAsync(cacheKey, document, TimeSpan.FromMinutes(30));
            }
            catch (Exception ex)
            {
                // Log lỗi Redis, không chặn luồng chính
                Console.WriteLine($"[Redis Error] Failed to set cache for doc {document.DocId}: {ex.Message}");
            }
        }

        // 4. Xóa tài liệu khỏi MongoDB
        public async Task<bool> DeleteAsync(int docId)
        {
            var result = await _claimDocumentsCollection.DeleteOneAsync(d => d.DocId == docId);
            bool isDeleted = result.IsAcknowledged && result.DeletedCount > 0;

            if (isDeleted)
            {
                try
                {
                    // Xóa cache để tránh trả về dữ liệu cũ đã bị xóa
                    await _cache.RemoveAsync($"claim_doc:{docId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Redis Error] Failed to remove cache for doc {docId}: {ex.Message}");
                }
            }

            return isDeleted;
        }
    }
}
