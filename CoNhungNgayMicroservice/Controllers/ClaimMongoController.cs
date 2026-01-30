using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;
using MongoDBCore.Services;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimMongoController : ControllerBase
    {
        private readonly IClaimRepository _repo;
        private readonly ICacheService _cache;

        public ClaimMongoController(IClaimRepository repo, ICacheService cache)
        {
            _repo = repo;
            _cache = cache;
        }

        [HttpPost("sync-from-oracle")]
        public async Task<IActionResult> SyncClaim([FromBody] ClaimSyncDto dto)
        {
            if (dto == null) return BadRequest("Dữ liệu đồng bộ trống.");

            try
            {
                // 1. Ghi vào MongoDB (Source of Truth cho phần Read)
                await _repo.UpsertClaimAsync(dto);

                // 2. Làm mới Redis Cache
                // Chúng ta xóa key cũ để lần Query tiếp theo từ GraphQL sẽ vào Mongo lấy data mới nhất
                string cacheKey = $"claim:{dto.ClaimId}";
                await _cache.RemoveAsync(cacheKey);

                // Hoặc chuyên nghiệp hơn: Ghi đè trực tiếp vào Redis để GraphQL không cần chạm vào Mongo nữa
                // await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30));

                return Ok(new { message = "Đồng bộ dữ liệu bồi thường và làm mới Cache thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi lưu trữ MongoDB/Cache: {ex.Message}");
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
