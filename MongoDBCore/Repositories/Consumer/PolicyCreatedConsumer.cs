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
    public class PolicyCreatedConsumer : IConsumer<PolicyCreatedEvent>
    {
        private readonly IPolicyRepository _policyRepository;

        public PolicyCreatedConsumer(IPolicyRepository policyRepository)
        {
            _policyRepository = policyRepository;
        }

        public async Task Consume(ConsumeContext<PolicyCreatedEvent> context)
        {
            var eventData = context.Message;

            var dto = new PolicyDto
            {
                PolicyId = eventData.PolicyId,
                PolicyNumber = eventData.PolicyNumber,
                CustomerId = eventData.CustomerId,     // Bây giờ sẽ hết lỗi nếu đã thêm vào Event
                AgentId = eventData.AgentId,           // Thêm vào Event luôn nhé
                InsTypeId = eventData.InsTypeId,       // Thêm vào Event luôn nhé
                CustomerName = eventData.CustomerName,
                AgentName = eventData.AgentName,
                InsTypeName = eventData.InsTypeName,
                StartDate = eventData.StartDate,
                EndDate = eventData.EndDate,
                PremiumAmount = eventData.PremiumAmount,
                Status = eventData.Status
            };

            switch (eventData.Action.ToUpper()) //// field action cua Event, dc truyen tu` Controller Oracle
            {
                case "CREATE":
                case "UPDATE":
                    await _policyRepository.UpsertAsync(dto);
                    break;
                case "DELETE":
                    await _policyRepository.DeleteAsync(eventData.PolicyId);
                    break;
            }
        }
    }
}
