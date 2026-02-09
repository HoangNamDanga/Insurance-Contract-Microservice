using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services
{
    public  interface IClaimService
    {
        // Trả về một đối tượng chứa trạng thái thành công và thông báo lỗi nếu có
        Task<(bool IsSuccess, string Message, int? ClaimId)> SubmitClaimAsync(ClaimCreateDto dto);


        // Nghiệp vụ mới: Duyệt hoặc từ chối bồi thường
        Task<(bool IsSuccess, string Message)> ProcessClaimStatusAsync(int claimId, string status, decimal? amountApproved, string note);


        //Hủy yêu cầu bồi thường
        Task<(bool IsSuccess, string Message)> CancelClaimAsync(int claimId, string reason);
    }
}
