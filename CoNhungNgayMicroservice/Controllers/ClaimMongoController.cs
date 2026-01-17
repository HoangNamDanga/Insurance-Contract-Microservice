using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimMongoController : ControllerBase
    {
        private readonly IClaimRepository _repo;

        public ClaimMongoController(IClaimRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("sync-from-oracle")]
        public async Task<IActionResult> SyncClaim([FromBody] ClaimSyncDto dto) // mongoDB dùng cách của mongoDB để hứng dữ liệu nên chỗ này mang giá trị của mongoDB, Oracle chỉ gửi sang Json
        {
            if (dto == null) return BadRequest("Dữ liệu đồng bộ trống.");

            try
            {
                await _repo.UpsertClaimAsync(dto);
                return Ok(new { message = "Đồng bộ dữ liệu bồi thường thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi lưu trữ MongoDB: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClaimById(int id)
        {
            try
            {
                // Gọi xuống Repository - nơi đã cài đặt logic Redis Cache
                var claim = await _repo.GetByIdAsync(id);

                if (claim == null)
                {
                    return NotFound(new { message = $"Không tìm thấy yêu cầu bồi thường ID: {id}" });
                }

                return Ok(claim);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }
}
