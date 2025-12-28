using MassTransit;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using static MassTransit.ValidationResultExtensions;

namespace MongoDBCore.Repositories.Consumer
{
    public class AgentCreatedConsumer : IConsumer<AgentEvent>
    {
        private readonly IAgentRepository _repo;

        public AgentCreatedConsumer(IAgentRepository repo)
        {
            _repo = repo;
        }

        //CREATE/UPDATE/DELETE gọi từ controller của Oracle
        public async Task Consume(ConsumeContext<AgentEvent> context)
        {
            var evenData = context.Message;
            Console.WriteLine($"DEBUG: Chuan bi gui Agent voi ID = {evenData.AgentId}");
            var dto = new AgentDto
            {
                AgentId = evenData.AgentId,
                FullName = evenData.FullName,
                Email = evenData.Email,
                Phone = evenData.Phone
            };

            switch (evenData.Action.ToUpper()) //// field action cua Event, dc truyen tu` Controller Oracle
            {
                case "CREATE": 
                case "UPDATE":
                    await _repo.UpsertAsync(dto);
                    break;
                case "DELETE":
                    await _repo.DeleteAsync(evenData.AgentId);
                    break;
            }
        }
    }
}
