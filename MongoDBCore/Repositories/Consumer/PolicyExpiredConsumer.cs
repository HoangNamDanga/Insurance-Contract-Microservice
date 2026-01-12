using DnsClient.Internal;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Email;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories.Consumer
{
    //Consumer cho NGHIỆP VỤ HẾT HẠN
    public class PolicyExpiredConsumer : IConsumer<PolicyExpiredEvent>
    {
        private readonly ILogger<PolicyExpiredConsumer> _logger;
        private readonly IEmailService _emailService;
        public PolicyExpiredConsumer(ILogger<PolicyExpiredConsumer> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<PolicyExpiredEvent> context)
        {
            foreach(var policy in context.Message.ExpiredPolicies)
            {
                _logger.LogInformation("Đang gửi email thông báo hết hạn cho : {0} ({1})", policy.CustomerName, policy.CustomerEmail);

                string content = $"Chào {policy.CustomerName}, Hợp đồng {policy.PolicyNumber} của bạn đã hết hạn ngày {policy.EndDate:dd/MM/yyyy}";

                await _emailService.SendEmailAsync(policy.CustomerEmail, "Thông báo hêt hạn bảo hiểm", content);
            }
        }
    }
}
