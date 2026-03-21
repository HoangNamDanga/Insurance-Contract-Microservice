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
                CustomerId = eventData.CustomerId,
                AgentId = eventData.AgentId,
                InsTypeId = eventData.InsTypeId,
                CustomerName = eventData.CustomerName,
                AgentName = eventData.AgentName,
                InsTypeName = eventData.InsTypeName,
                StartDate = eventData.StartDate,
                EndDate = eventData.EndDate,
                PremiumAmount = eventData.PremiumAmount,
                Status = eventData.Status,

                // SỬA LỖI CS0029 TẠI ĐÂY: Khởi tạo đối tượng mới và gán từng field
                Vehicle = eventData.Vehicle == null ? null : new PolicyDto.VehicleInfo
                {
                    Brand = eventData.Vehicle.Brand,
                    Model = eventData.Vehicle.Model
                },

                // SỬA LỖI CS0029 CHO DANH SÁCH CLAIMS
                Claims = eventData.Claims?.Select(c => new PolicyDto.ClaimInfo
                {
                    ClaimId = c.ClaimId,
                    AmountApproved = c.AmountApproved,
                    Status = c.Status
                }).ToList() ?? new List<PolicyDto.ClaimInfo>()
            };

            switch (eventData.Action?.ToUpper())
            {
                case "CREATE":
                case "UPDATE":
                    // Hàm UpsertAsync lúc này sẽ nhận dto có đầy đủ Vehicle và Claims
                    await _policyRepository.UpsertAsync(dto);
                    break;
                case "DELETE":
                    await _policyRepository.DeleteAsync(eventData.PolicyId);
                    break;
            }
        }
    }
}
