using MongoDB.Driver;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;

namespace CoNhungNgayMicroservice.GraphQL
{
    public class Query
    {
        // 1. Lấy toàn bộ danh sách Policy
        // [Service] giúp HotChocolate tự động lấy IMongoCollection từ DI Container đã cấu hình ở Program.cs
        // Sử dụng Repository thay vì gọi trực tiếp vào DB
        public async Task<IEnumerable<PolicyDto>> GetPolicies([Service] IPolicyRepository repository)
        {
            // Giả sử interface của bạn có hàm GetAllAsync()
            return await repository.GetAllAsync();
        }

        public async Task<PolicyDto?> GetPolicyById(int id, [Service] IPolicyRepository repository)
        {
            // Giả sử interface của bạn có hàm GetByIdAsync()
            // Repository này đã tự xử lý logic lấy từ Redis hay MongoDB rồi!
            return await repository.GetByIdAsync(id);
        }


        public async Task<ClaimSyncDto?> GetClaimById(
            int id, [Service] IClaimRepository repository)
        {
            // Khi gọi hàm này, nó sẽ: Check Redis -> Nếu hụt thì check Mongo -> Lưu Redis -> Trả về
            return await repository.GetByIdAsync(id);
        }
    }
}
