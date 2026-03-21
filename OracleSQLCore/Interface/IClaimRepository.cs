using OracleSQLCore.Models.DTOs;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Interface
{
    public interface IClaimRepository
    {

        // 1. Khai báo hàm lấy PolicyId từ ClaimId
        Task<int> GetPolicyIdByClaimId(int claimId);

        // 2. Khai báo hàm làm giàu dữ liệu (Enrich)
        // Lưu ý: Nếu ở Repo bạn đang để private, hãy đổi nó thành public mới gọi được từ Service
        Task<PolicyCreatedEvent> EnrichPolicyData(int id, string action);


        Task<PolicyCreatedEvent> AddClaimAsync(ClaimCreateDto dto);

        // Một hàm riêng để lấy dữ liệu "phẳng" phục vụ đồng bộ
        Task<ClaimSyncDto> GetClaimForSyncAsync(int claimId);


        // 2. Nghiệp vụ Duyệt/Từ chối bồi thường (Approve/Reject)
        // Cập nhật trạng thái và số tiền được duyệt
        Task<bool> UpdateClaimStatusAsync(int claimId, string status, decimal? amountApproved, string description);

        // 3. Nghiệp vụ Hủy yêu cầu (Cancel Claim)
        // Thường chỉ cho phép khi trạng thái đang là 'PENDING'
        Task<bool> CancelClaimAsync(int claimId, string reason);

        // 4. Nghiệp vụ Tính tổng tiền đã yêu cầu bồi thường (Calculate Total Claimed Amount)
        // Phục vụ kiểm tra hạn mức của hợp đồng (Policy Limit)
        Task<decimal> GetTotalClaimedAmountByPolicyIdAsync(int policyId);
    }
}
