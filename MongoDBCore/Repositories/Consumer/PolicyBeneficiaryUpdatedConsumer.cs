using MassTransit;
using MongoDBCore.Interfaces;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories.Consumer
{
    public class PolicyBeneficiaryUpdatedConsumer : IConsumer<PolicyBeneficiaryUpdatedEvent>
    {
        private readonly IPolicyBeneficiaryRepository _policyBeneficiaryRepository;

        public PolicyBeneficiaryUpdatedConsumer(IPolicyBeneficiaryRepository policyBeneficiaryRepository)
        {
            _policyBeneficiaryRepository = policyBeneficiaryRepository;
        }


        public async Task Consume(ConsumeContext<PolicyBeneficiaryUpdatedEvent> context)
        {
            var message = context.Message;

            // Map từ UpdatedEvent sang CreatedEvent để dùng chung hàm Upsert (tiết kiệm code)
            var data = new PolicyBeneficiaryCreatedEvent
            {
                BeneficiaryId = message.BeneficiaryId,
                PolicyId = message.PolicyId,
                FullName = message.FullName,
                Relationship = message.Relationship,
                Phone = message.Phone,
                CreatedAt = message.UpdatedAt // Lấy mốc thời gian mới nhất
            };

            await _policyBeneficiaryRepository.UpsertPolicyBeneficiaryAsync(message.PolicyId, data);
        }
    }
}
