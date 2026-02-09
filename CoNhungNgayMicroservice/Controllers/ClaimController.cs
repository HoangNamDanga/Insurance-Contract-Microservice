using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Services;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimController : ControllerBase
    {
        private readonly IClaimService _claimService;

        public ClaimController(IClaimService claimService)
        {
            _claimService = claimService;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitClaim([FromBody] ClaimCreateDto dto)
        {
            if (dto == null) return BadRequest("Dữ liệu không hợp lệ.");

            // Gọi Service để xử lý trọn gói: Lưu Oracle -> Check Trigger -> Sync sang Mongo
            var result = await _claimService.SubmitClaimAsync(dto);

            if (!result.IsSuccess)
            {
                // Trả về lỗi nghiệp vụ (ví dụ: Hợp đồng hết hạn - 400 Bad Request)
                return BadRequest(new { message = result.Message });
            }

            // Trả về thành công kèm ID hồ sơ mới tạo
            return Ok(new
            {
                message = result.Message,
                claimId = result.ClaimId
            });
        }


        /// <summary>
        /// Duyệt hoặc Từ chối yêu cầu bồi thường
        /// </summary>
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateClaimStatus([FromBody] ClaimUpdateStatusRequest request)
        {
            // 1. Gọi Service để xử lý nghiệp vụ (Oracle -> Sync MongoDB)
            var result = await _claimService.ProcessClaimStatusAsync(
                request.ClaimId,
                request.Status,
                request.AmountApproved,
                request.Description);

            // 2. Trả về kết quả dựa trên logic nghiệp vụ
            if (result.IsSuccess)
            {
                return Ok(new { message = result.Message });
            }

            // Nếu thất bại (do lỗi nghiệp vụ Oracle như: duyệt quá tiền, hợp đồng hết hạn...)
            return BadRequest(new { message = result.Message });
        }


        /// <summary>
        /// Hủy yêu cầu bồi thường (Chỉ áp dụng cho hồ sơ PENDING)
        /// </summary>
        [HttpPatch("cancel")]
        public async Task<IActionResult> CancelClaim([FromBody] ClaimCancelRequest request)
        {
            if (request == null || request.ClaimId <= 0)
                return BadRequest("Thông tin yêu cầu không hợp lệ.");

            // Gọi Service để thực hiện: Oracle (Procedure) -> Sync MongoDB
            var result = await _claimService.CancelClaimAsync(request.ClaimId, request.Reason);

            if (result.IsSuccess)
            {
                return Ok(new { message = result.Message });
            }

            // Trả về lỗi nếu hồ sơ không ở trạng thái PENDING hoặc lỗi hệ thống
            return BadRequest(new { message = result.Message });
        }
    }
}
