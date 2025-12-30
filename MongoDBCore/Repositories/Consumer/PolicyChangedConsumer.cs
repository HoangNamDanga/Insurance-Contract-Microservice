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
    public class PolicyChangedConsumer : IConsumer<PolicyChangedEvent>
    {
        private readonly IPolicyRepository _repository;

        public PolicyChangedConsumer(IPolicyRepository repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<PolicyChangedEvent> context)
        {
            var message = context.Message;

            // ----------------------------

            if (message.ActionType == "CANCEL")
            {
                await _repository.UpdateCancelStatusAsync(message.PolicyId, message.LastNotes);
            }
            else if (message.ActionType == "RENEW")
            {
                await _repository.UpdateRenewStatusAsync(message.PolicyId, message.EndDate, message.TotalPremium, message.LastNotes);
            }
        }
    }
}
