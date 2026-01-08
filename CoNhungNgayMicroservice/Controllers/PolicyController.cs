using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Interfaces;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Services;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyService _policyService;
        private readonly IPolicyRepository _policyRepository;
        public PolicyController(IPolicyService policyService, IPolicyRepository policyRepository)
        {
            _policyService = policyService;
            _policyRepository = policyRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _policyService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _policyService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PolicyDto dto)
        {
            // Service sẽ lo việc: Ghi Oracle -> Enrich Data -> Publish RabbitMQ (with Retry)
            var result = await _policyService.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PolicyDto dto)
        {
            dto.PolicyId = id;
            var result = await _policyService.UpdateAsync(dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _policyService.DeleteAsync(id);
            return Ok(new { Message = "Deleted successfully", PolicyId = id });
        }

        // GET: api/policies
        [HttpGet("mongo")]
        public async Task<ActionResult<List<PolicyDto>>> GetAllMongo()
        {
            var policies = await _policyRepository.GetAllAsync();
            return Ok(policies);
        }

        // GET: api/Policy/mongo/{id}
        [HttpGet("mongo/{id}")]
        public async Task<ActionResult<PolicyDto>> GetByIdMongo(int id)
        {
            var policy = await _policyRepository.GetByIdAsync(id);

            if (policy == null)
            {
                return NotFound(new { message = $"Không tìm thấy hợp đồng có ID {id} trong MongoDB" });
            }

            return Ok(policy);
        }

        // 1. Endpoint Nghiệp vụ Gia hạn
        [HttpPost("renew")]
        public async Task<IActionResult> Renew([FromBody] RenewPolicyDto request)
        {
            // 1. Kiểm tra tính hợp lệ của Request
            if (request == null || request.PolicyId <= 0)
            {
                return BadRequest("Thông tin gia hạn không hợp lệ.");
            }
            try
            {
                // 2. Thực hiện nghiệp vụ chính tại Oracle thông qua Service
                // Hàm RenewAsync này sẽ: Gọi Procedure -> Lưu History -> Publish tin nhắn qua RabbitMQ/Kafka
                var result = await _policyService.RenewAsync(request);

                if (result == null)
                    return NotFound($"Không tìm thấy hợp đồng ID {request.PolicyId} để gia hạn.");

                // 3. Trả về kết quả thành công cho người dùng
                // Lúc này, Consumer ở một tiến trình khác sẽ nhận tin nhắn, cập nhật Mongo và XÓA CACHE
                return Ok(new
                {
                    Message = "Gia hạn hợp đồng thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                // Phân tích lỗi từ Oracle gửi về (ví dụ ORA-20001 đã chặn ở Database)
                if (ex.Message.Contains("ORA-20001"))
                {
                    return BadRequest($"Lỗi nghiệp vụ: {ex.Message}");
                }

                // Các lỗi hệ thống khác
                return StatusCode(500, $"Lỗi hệ thống khi gia hạn: {ex.Message}");
            }
        }

        // 2. Endpoint Nghiệp vụ Hủy hợp đồng
        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel([FromBody] CancelPolicyDto request)
        {
            if (request == null || request.PolicyId <= 0)
            {
                return BadRequest("Thông tin hủy hợp đồng không hợp lệ.");
            }

            try
            {
                var result = await _policyService.CancelAsync(request);

                if (result == null)
                    return NotFound($"Không tìm thấy hợp đồng ID {request.PolicyId} để hủy.");

                return Ok(new
                {
                    Message = "Hủy hợp đồng thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                // Log error here
                return StatusCode(500, $"Lỗi hệ thống khi hủy: {ex.Message}");
            }
        }

        //End point Search MongoDB
        [HttpGet("search")]
        public async Task<IActionResult> Search (
            [FromQuery] string? customerName,
            [FromQuery] string? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                //Goi xuống repository để thực hiện tìm kiếm 
                var reuslts = await _policyRepository.SearchAsync(customerName, status, fromDate, toDate);

                if(reuslts == null || reuslts.Count == 0)
                {
                    return Ok(new { Message = "Khong tim thay ket qua phu hop.", Data = reuslts });
                }

                return Ok(new
                {
                    TotalFound = reuslts.Count,
                    Data = reuslts
                });
            }catch(Exception ex)
            {
                return StatusCode(500, $"Lỗi khi tìm kiếm: {ex.Message}" );
            }
        }
    }
}
