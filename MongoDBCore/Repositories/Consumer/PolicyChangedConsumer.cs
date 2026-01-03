using MassTransit;
using MongoDBCore.Interfaces;
using MongoDBCore.Services;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories.Consumer
{
    public class PolicyChangedConsumer : IConsumer<PolicyChangedEvent>
    {
        private readonly IPolicyRepository _repository;
        private readonly ICacheService _cacheService; // Thêm Cache Service

        public PolicyChangedConsumer(IPolicyRepository repository, ICacheService cacheService)
        {
            _repository = repository;
            _cacheService = cacheService;
        }

        public async Task Consume(ConsumeContext<PolicyChangedEvent> context)
        {
            var message = context.Message;
            string cacheKey = $"policy:{message.PolicyId}";

            if (message.ActionType == "CANCEL")
            {
                // 1. Cập nhật MongoDB
                await _repository.UpdateCancelStatusAsync(message.PolicyId, message.LastNotes);

                // 2. Xóa Cache ngay lập tức để người dùng không thấy trạng thái cũ
                await _cacheService.RemoveAsync(cacheKey);
            }
            else if (message.ActionType == "RENEW")
            {
                // 1. Cập nhật MongoDB
                await _repository.UpdateRenewStatusAsync(
                    message.PolicyId,
                    message.EndDate,
                    message.TotalPremium,
                    message.LastNotes);

                // 2. Xóa Cache
                await _cacheService.RemoveAsync(cacheKey);
            }
        }
    }
}
