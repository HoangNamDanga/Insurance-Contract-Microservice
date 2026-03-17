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
    public class PaymentCreatedConsumer : IConsumer<PaymentCreatedEvent>
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentCreatedConsumer(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task Consume(ConsumeContext<PaymentCreatedEvent> context)
        {
            var msg = context.Message;

            var paymentDto = new PaymentDto
            {
                PaymentId = msg.PaymentId,
                PolicyId = msg.PolicyId,
                Amount = msg.Amount,
                Method = msg.Method,
                Status = msg.Status ?? "Pending",
                PaymentPeriod = msg.PaymentPeriod,
                CreateAt = msg.CreatedAt,
                PaymentDate = msg.Timestamp
            };

            // Hàm này của bạn đã có: Lưu Mongo + Set Redis
            await _paymentRepository.UpsertPaymentAsync(paymentDto);

            Console.WriteLine($"[MongoDB] Created: {msg.PaymentId} | Policy: {msg.PolicyId}");
        }
    }
}
