using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Interfaces;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Repositories.BackgroundServices;
using OracleSQLCore.Services;
using System.Data;
using System.Net.Http;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyService _policyService;
        private readonly IPolicyRepository _policyRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PolicyController> _logger; // Khai báo thêm logger
        public PolicyController(ILogger<PolicyController> logger,IPolicyService policyService, IPolicyRepository policyRepository, IHttpClientFactory httpClientFactory)
        {
            _policyService = policyService;
            _policyRepository = policyRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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


        /// <summary>
        /// API Xác nhận thanh toán phí bảo hiểm
        /// </summary>
        /// <param name="paymentId">ID của bản ghi thanh toán (DHN_PAYMENT)</param> // RabbitMQ
        [HttpPost("confirm-payment-transaction/{paymentId:int}")]
        public async Task<IActionResult> ConfirmPayment(int paymentId)
        {
            _logger.LogInformation("Bắt đầu xử lý xác nhận thanh toán cho PaymentID: {PaymentId}", paymentId);

            try
            {
                // Gọi Service để thực hiện chuỗi nghiệp vụ tại Oracle và bắn Event
                var result = await _policyService.ConfirmPaymentAsync(paymentId);

                if (result == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin thanh toán hoặc xử lý thất bại cho ID: {PaymentId}", paymentId);
                    return NotFound(new { message = "Không tìm thấy thông tin thanh toán hoặc giao dịch không hợp lệ." });
                }

                // Trả về thành công cùng thông tin tóm tắt
                return Ok(new
                {
                    message = "Xác nhận thanh toán thành công.",
                    policyId = result.PolicyId,
                    status = result.NewPolicyStatus,
                    agentRank = result.NewAgentLevel,
                    commission = result.CommissionAmount,
                    processedAt = result.ProcessedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi xác nhận thanh toán cho ID: {PaymentId}", paymentId);

                // Trả về lỗi 500 nếu có sự cố hệ thống
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống khi xử lý thanh toán.", details = ex.Message });
            }
        }



        [HttpPost("trigger-expire")]
        public async Task<IActionResult> Trigger([FromServices] PolicyWorker worker)
        {
            try
            {
                // Gọi hàm thực thi trọn gói của Worker (bao gồm Oracle -> RabbitMQ -> Email)
                await worker.ExecuteInternal();

                return Ok(new { message = "Đã kích hoạt quét trọn gói và gửi thông báo thành công." });
            }
            catch (Exception ex)
            {
                // Trả về lỗi chi tiết nếu có vấn đề xảy ra
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }



        [HttpPost("confirm/{policyId}")] // api này sẽ đồng bộ với CommisstionMongo Controller HTTP
        public async Task<IActionResult> ConfirmAndSync(int policyId)
        {
            // BƯỚC 1: Xử lý tại Oracle (Trigger tự động tính hoa hồng bên trong Repository)
            // Hàm này trả về CommissionSyncDto chứa: PolicyId, AgentName, CustomerName, CommissionAmount, v.v.
            var syncData = await _policyService.ConfirmAndGetCommissionAsync(policyId);

            if (syncData == null)
            {
                return BadRequest("Không thể xác nhận thanh toán hoặc hợp đồng đã được xử lý trước đó.");
            }

            // BƯỚC 2: Sử dụng HttpClient từ Factory (có cấu hình Polly để Retry nếu mạng lag)
            var client = _httpClientFactory.CreateClient("MongoSyncClient");

            // URL phía MongoDB nhận dữ liệu hoa hồng
            string mongoUrl = "http://api:8080/api/CommissionMongo/sync-commission";

            try
            {
                // Gửi toàn bộ cục "syncData" (đã làm giàu dữ liệu) sang MongoDB
                var response = await client.PostAsJsonAsync(mongoUrl, syncData);

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        Status = "Success",
                        Message = "Xác nhận thanh toán và đồng bộ hoa hồng thành công!",
                        Data = syncData
                    });
                }
                // Trường hợp API Mongo phản hồi lỗi (400, 500...)
                var errorBody = await response.Content.ReadAsStringAsync();
                return Accepted(new
                {
                    Status = "PartialSuccess",
                    Warning = $"Oracle đã lưu nhưng Mongo từ chối: {errorBody}",
                    Data = syncData
                });
            }
            catch (HttpRequestException ex)
            {
                // Trường hợp Service Mongo chết hẳn hoặc timeout sau khi Polly đã retry
                return Accepted(new
                {
                    Status = "PartialSuccess",
                    Warning = $"Lỗi kết nối Mongo service: {ex.Message}. Dữ liệu Oracle vẫn được bảo toàn.",
                    Data = syncData
                });
            }
        }



    }
}
