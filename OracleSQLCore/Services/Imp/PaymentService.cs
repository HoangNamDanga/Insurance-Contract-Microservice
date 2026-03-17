using MassTransit;
using MongoDBCore.Services;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using Polly.Retry;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services.Imp
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ICacheService _cacheService; // Thêm Cache Service
        public PaymentService(
            IPaymentRepository paymentRepository,
            IPublishEndpoint publishEndpoint,
            AsyncRetryPolicy retryPolicy,
            ICacheService cacheService)
        {
            _paymentRepository = paymentRepository;
            _publishEndpoint = publishEndpoint;
            _retryPolicy = retryPolicy;
            _cacheService = cacheService;
        }

        public async Task<bool> CompletePaymentAsync(decimal paymentId, string status, string transactionId)
        {
            bool isUpdated = await _retryPolicy.ExecuteAsync(async () =>
                await _paymentRepository.UpdateStatusAsync(paymentId, status, transactionId)
            );

            if (isUpdated)
            {
                // QUAN TRỌNG: Xóa cache cũ để khách hàng thấy trạng thái mới ngay lập tức
                await _cacheService.RemoveAsync($"payment:{paymentId}");

                var updatedPayment = await _paymentRepository.GetByIdAsync(paymentId);

                await _publishEndpoint.Publish<PaymentStatusUpdatedEvent>(new
                {
                    PaymentId = paymentId,
                    NewStatus = status,
                    TransactionId = transactionId,
                    updatedPayment.PolicyId,
                    updatedPayment.Amount,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            return isUpdated;
        }

        public async Task<decimal> CreatePaymentTransactionAsync(PaymentDto paymentDto)
        {
            // Retry
            decimal paymentId = await _retryPolicy.ExecuteAsync(async () =>
                await _paymentRepository.CreatePaymentAsync(paymentDto));

            if(paymentId > 0)
            {
                paymentDto.PaymentId = paymentId;
                paymentDto.Status = "Pending";

                // Publish Event sang RabbitMQ để các Consumer (MongoDB, Email, SMS) xử lý

                await _publishEndpoint.Publish<PaymentCreatedEvent>(new
                {
                    PaymentId = paymentId, // Vẫn sáng vì tên khác nhau (PaymentId vs paymentId)
                    paymentDto.PolicyId,   // Code sẽ sáng lại vì đây là cách viết rút gọn chuẩn vì là tên giống với class nên thế này là gán luôn
                    paymentDto.Amount,     
                    paymentDto.Method,     
                    Status = "Pending",    
                    paymentDto.PaymentPeriod, 
                    CreatedAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow
                });
            }

            return paymentId;
        }

        public async Task<PaymentDto?> GetPaymentDetailsAsync(decimal paymentId)
        {
            string cacheKey = $"payment:{paymentId}";

            // 1. Kiểm tra Cache (Redis)
            var cachedPayment = await _cacheService.GetAsync<PaymentDto>(cacheKey);
            if (cachedPayment != null)
            {
                return cachedPayment; // Trả về ngay nếu thấy trong Cache
            }

            // 2. Nếu Cache không có (Cache Miss), gọi Repo lấy từ Oracle
            var payment = await _paymentRepository.GetByIdAsync(paymentId);

            // 3. Nếu tìm thấy trong Oracle, lưu lại vào Cache cho lần sau
            if (payment != null)
            {
                await _cacheService.SetAsync(cacheKey, payment, TimeSpan.FromMinutes(10));
            }

            return payment;
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentHistoryByPolicyAsync(decimal policyId)
        {
            // Lấy danh sách lịch sử thanh toán của hợp đồng
            return await _paymentRepository.GetByPolicyIdAsync(policyId);
        }
    }
}
