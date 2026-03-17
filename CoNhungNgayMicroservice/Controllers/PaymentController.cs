using Microsoft.AspNetCore.Mvc;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Services;

namespace CoNhungNgayMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PaymentController(
            IPaymentService paymentService,
            IHttpClientFactory httpClientFactory, // Sử dụng Factory để quản lý connection tốt hơn
            IConfiguration configuration)
        {
            _paymentService = paymentService;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        /// <summary>
        /// API để Frontend gọi khi khách hàng nhấn "Thanh Toán"
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] PaymentDto paymentDto)
        {
            // 1. Tạo Transaction trong Oracle (Trạng thái Pending) và bắn Event PaymentCreated
            decimal paymentId = await _paymentService.CreatePaymentTransactionAsync(paymentDto);

            if (paymentId <= 0) return BadRequest("Lỗi khởi tạo giao dịch.");

            // 2. Lấy URL MockBank từ cấu hình Docker (http://mockbank:8080/...)
            var bankApiUrl = _configuration["BankSettings:ApiUrl"];

            // 3. Gửi lệnh sang MockBank để xử lý thanh toán thực tế
            var bankRequest = new { PaymentId = paymentId, Amount = paymentDto.Amount };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(bankApiUrl, bankRequest); // gọi api của ngân hàng/momo
                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { Message = "Yêu cầu đã gửi tới Ngân hàng", PaymentId = paymentId });
                }
                return StatusCode(502, "Ngân hàng không phản hồi.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi kết nối hệ thống ngân hàng: {ex.Message}");
            }
        }

        /// <summary>
        /// API để MockBank gọi ngược lại sau khi khách hàng nhập OTP thành công (Callback)
        /// </summary>
        [HttpPost("callback")]
        public async Task<IActionResult> BankCallback([FromBody] BankResponse response)
        {
            // Cập nhật trạng thái Success/Failed, Xóa Cache, và bắn Event StatusUpdated
            bool isSuccess = await _paymentService.CompletePaymentAsync(
                response.PaymentId,
                response.Status,
                response.TransactionId);

            if (isSuccess)
            {
                return Ok(new { Message = "Cập nhật kết quả thanh toán thành công." });
            }
            return BadRequest("Giao dịch không tồn tại hoặc đã được xử lý trước đó.");
        }

        /// <summary>
        /// API lấy lịch sử thanh toán theo Hợp đồng (Policy)
        /// </summary>
        [HttpGet("history/{policyId}")]
        public async Task<IActionResult> GetHistory(decimal policyId)
        {
            var history = await _paymentService.GetPaymentHistoryByPolicyAsync(policyId);
            return Ok(history);
        }

        /// <summary>
        /// API lấy chi tiết 1 giao dịch (Tự động dùng Redis Cache nếu có)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(decimal id)
        {
            var detail = await _paymentService.GetPaymentDetailsAsync(id);
            return detail != null ? Ok(detail) : NotFound();
        }
    }

    // Model để nhận dữ liệu từ MockBank
    public record BankResponse(decimal PaymentId, string Status, string TransactionId);
}