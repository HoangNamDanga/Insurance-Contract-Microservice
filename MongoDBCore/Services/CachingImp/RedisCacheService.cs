using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Services.CachingImp
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var jsonData = await _cache.GetStringAsync(key);
            return jsonData == null ? default : JsonConvert.DeserializeObject<T>(jsonData);
        }

        public async  Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            //1. Kiểm tra trong Redis
            var cacheData = await _cache.GetStringAsync(key);

            if (!string.IsNullOrEmpty(cacheData))
            {
                Console.WriteLine($"[REDIS] Cache Hit cho key: {key}"); // Thêm dòng này
                return JsonConvert.DeserializeObject<T>(cacheData);
            }

            //2. Nếu không có, thực hiện hàm factory (truy vấn MongoDb)
            var data = await factory();

            //3. Nếu có dữ liệu từ Db, lưu vào Redis
            if(data != null)
            {
                Console.WriteLine($"[REDIS] Cache Miss. Đang lưu key: {key} vào Redis..."); // Thêm dòng này
                await SetAsync(key, data, expiry);
            }

            return data;
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                // Mặc định 10 phút nếu không truyền thời gian hết hạn
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(10)
            };

            var jsonData = JsonConvert.SerializeObject(value);
            await _cache.SetStringAsync(key, jsonData, options);
        }
    }
}
