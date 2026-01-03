using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Services
{
    public interface ICacheService
    {
        // Hàm lấy dữ liệu hoặc tự động Set nếu chưa có (Pattern: Cache-Aside)
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);

        // Hàm xóa Cache (Dùng khi Consumer cập nhật MongoDB thành công)
        Task RemoveAsync(string key);

        // Hàm Set thủ công nếu cần
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

        // Hàm Get thủ công
        Task<T?> GetAsync<T>(string key);
    }
}
/*
 public async Task<PolicyDto> GetByIdAsync(int id)
    {
        string cacheKey = $"policy:{id}";

        // Chỉ 1 dòng: Tự check Redis, nếu trống tự gọi Lambda function xuống MongoDB
        return await _cacheService.GetOrSetAsync(cacheKey, async () => 
        {
            return await _policiesCollection.Find(x => x.PolicyId == id).FirstOrDefaultAsync();
        }, TimeSpan.FromMinutes(30));
  }
 */