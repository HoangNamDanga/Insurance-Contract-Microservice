using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Interface
{
    public interface IClaimRepository
    {
        Task<int> AddClaimAsync(ClaimCreateDto dto);

        // Một hàm riêng để lấy dữ liệu "phẳng" phục vụ đồng bộ
        Task<ClaimSyncDto> GetClaimForSyncAsync(int claimId);
    }
}
