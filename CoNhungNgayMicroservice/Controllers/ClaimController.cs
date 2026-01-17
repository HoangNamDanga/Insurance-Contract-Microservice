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
    }
}
