using MongoDBCore.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Interfaces
{
    public interface IClaimRepository
    {
        Task UpsertClaimAsync(ClaimSyncDto claimDoc);
        Task<IEnumerable<ClaimSyncDto>> GetClaimsByCustomerAsync(string customerName);

        // Thêm hàm để sửu dụng cache
        Task<ClaimSyncDto> GetByIdAsync(int claimId);

        // Thêm hàm để sửu dụng cache , khi xóa 1 thằng thì đảm bảo dữ liệu bị xóa ở mọi nơi
        //Task<bool> DeleteAsync(int claimId)
    }
}
