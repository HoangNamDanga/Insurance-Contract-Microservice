using MassTransit;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories.Consumer
{
    public class PaymentStatusUpdatedConsumer : IConsumer<PaymentStatusUpdatedEvent>
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentStatusUpdatedConsumer(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task Consume(ConsumeContext<PaymentStatusUpdatedEvent> context)
        {
            var msg = context.Message;

            // 1. Tìm bản ghi hiện tại
            var existing = await _paymentRepository.GetByIdAsync(msg.PaymentId);

            if (existing == null)
            {
                // Nếu chưa có (do CreatedEvent đến chậm), ta tạo mới luôn nhờ các trường bạn vừa thêm
                existing = new PaymentDto
                {
                    PaymentId = msg.PaymentId,
                    PolicyId = msg.PolicyId,
                    Amount = msg.Amount,
                    CreateAt = msg.UpdatedAt // Tạm thời dùng UpdatedAt
                };
            }

            // 2. Cập nhật thông tin mới nhất từ Event
            existing.Status = msg.NewStatus;
            existing.TransactionId = msg.TransactionId;
            existing.PaymentDate = msg.UpdatedAt;

            // 3. Lưu (Hàm này sẽ ghi đè vào Mongo và xóa/set lại Redis)
            await _paymentRepository.UpsertPaymentAsync(existing);

            Console.WriteLine($"[MongoDB] Updated: {msg.PaymentId} -> {msg.NewStatus}");
        }
    }
}
