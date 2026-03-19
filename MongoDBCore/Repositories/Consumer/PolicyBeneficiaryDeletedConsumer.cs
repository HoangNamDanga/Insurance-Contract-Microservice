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
    public class PolicyBeneficiaryDeletedConsumer : IConsumer<PolicyBeneficiaryDeletedEvent>
    {
        private readonly IPolicyBeneficiaryRepository _policyBeneficiaryRepository;

        public PolicyBeneficiaryDeletedConsumer(IPolicyBeneficiaryRepository policyBeneficiaryRepository)
        {
            _policyBeneficiaryRepository = policyBeneficiaryRepository;
        }
        public async Task Consume(ConsumeContext<PolicyBeneficiaryDeletedEvent> context)
        {
            var message = context.Message;

            // Thực hiện xóa Document trong Mongo và xóa Cache tương ứng
            await _policyBeneficiaryRepository.RemovePolicyBeneficiaryAsync(message.PolicyId, message.BeneficiaryId);
        }
    }
}
