using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDBCore.Interfaces;
using MongoSync.Service.Consumers;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories.Consumer
{
    public class PolicyBeneficiaryCreatedConsumer : IConsumer<PolicyBeneficiaryCreatedEvent>
    {
        private readonly IPolicyBeneficiaryRepository _policyBeneficiaryRepository;
        private readonly ILogger<PolicyBeneficiaryCreatedConsumer> _logger;

        public PolicyBeneficiaryCreatedConsumer(IPolicyBeneficiaryRepository policyBeneficiaryRepository, ILogger<PolicyBeneficiaryCreatedConsumer> logger)
        {
            _policyBeneficiaryRepository = policyBeneficiaryRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PolicyBeneficiaryCreatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("--> [MongoSync] Nhận CreatedEvent: Người thụ hưởng {BeneficiaryId}", message.BeneficiaryId);

            // Gọi Repository để Upsert và tự động xóa Cache
            await _policyBeneficiaryRepository.UpsertPolicyBeneficiaryAsync(message.PolicyId, message);
        }
    }
}
