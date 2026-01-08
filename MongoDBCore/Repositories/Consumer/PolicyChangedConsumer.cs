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

            // Dùng ToUpper() để so sánh cho chắc chắn
            string action = message.ActionType?.ToUpper();

            if (action == "CANCEL")
            {
                await _repository.UpdateCancelStatusAsync(message.PolicyId, message.LastNotes);
            }
            else if (action == "RENEW")
            {
                await _repository.UpdateRenewStatusAsync(
                    message.PolicyId,
                    message.EndDate,
                    message.TotalPremium,
                    message.LastNotes);
            }

            // Đưa ra ngoài này để bất kể Action nào cũng xóa Cache cho an toàn
            await _cacheService.RemoveAsync(cacheKey);

            Console.WriteLine($"[Consumer] Action {action} processed for Policy {message.PolicyId}. Cache cleared.");
        }
    }
}
