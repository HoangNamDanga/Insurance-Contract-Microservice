using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommissionMongoController : ControllerBase
    {
        private readonly ICommissionRepository _mongoRepository;
        private readonly ILogger<CommissionMongoController> _logger;

        public CommissionMongoController(ICommissionRepository mongoRepository, ILogger<CommissionMongoController> logger)
        {
            _mongoRepository = mongoRepository;
            _logger = logger;
        }

        [HttpPost("sync-commission")]
        public async Task<IActionResult> SyncFromOracle([FromBody] AgentCommissionMongo data)
        {
            if (data == null || data.PolicyId <= 0)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            try
            {
                await _mongoRepository.UpsertCommissionAsync(data);

                _logger.LogInformation($"Đồng bộ thành công hoa hồng cho Hợp đồng: {data.PolicyNumber}");

                return Ok(new { Message = "Đã nhận và cập nhật dữ liệu MongoDB" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi ghi dữ liệu vào MongoDB");
                return StatusCode(500, "Lỗi hệ thống lưu trữ MongoDB");
            }
        }
    }
}
