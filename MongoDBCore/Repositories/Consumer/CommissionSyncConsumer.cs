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
    public class CommissionSyncConsumer : IConsumer<PaymentConfirmedEvent>
    {
        private readonly ICommissionRepository _mongoRepo;

        public CommissionSyncConsumer(ICommissionRepository mongoRepo)
        {
            _mongoRepo = mongoRepo;
        }

        public async Task Consume(ConsumeContext<PaymentConfirmedEvent> context)
        {
            var eventData = context.Message;

            // Chuyển đổi dữ liệu sang Model AgentCommissionMongo của bạn
            var mongoData = new AgentCommissionMongo
            {
                PolicyId = eventData.PolicyId,
                PolicyNumber = eventData.PolicyNumber,
                CustomerName = eventData.CustomerName,
                AgentId = eventData.AgentId,
                AgentName = eventData.AgentName,
                TotalPayment = eventData.TotalPayment,
                CommissionAmount = eventData.CommissionAmount,
                Status = "SUCCESS", // Đánh dấu đã thanh toán và tính hoa hồng xong
                SyncDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                LastUpdatedAt = DateTime.Now
            };

            // Gọi hàm ghi đè theo PolicyId (Single Responsibility)
            await _mongoRepo.UpsertCommissionAsync(mongoData);
        }
    }
}
