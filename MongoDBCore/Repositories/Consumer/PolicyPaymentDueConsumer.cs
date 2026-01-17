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
    public class PolicyPaymentDueConsumer : IConsumer<PolicyPaymentDueEvent>
    {
        private readonly ILogger<PolicyPaymentDueConsumer> _logger;
        private readonly IEmailService _emailService;

        public PolicyPaymentDueConsumer(ILogger<PolicyPaymentDueConsumer> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        //luồng này chưa đồng bộ và lưu redis

        public async Task Consume(ConsumeContext<PolicyPaymentDueEvent> context)
        {
            foreach (var policy in context.Message.DuePolicies)
            {
                _logger.LogInformation("Đang gửi email NHẮC PHÍ cho: {0} ({1})", policy.FullName, policy.Email);

                // Template cho nhắc phí chuyên nghiệp hơn
                string content = $@"
                <h3>THÔNG BÁO ĐẾN HẠN THANH TOÁN</h3>
                Chào <b>{policy.FullName}</b>,<br/>
                Hợp đồng bảo hiểm số: <b>{policy.PolicyNumber}</b> của bạn sẽ đến hạn thanh toán vào ngày: {policy.EndDate:dd/MM/yyyy}.<br/>
                Số tiền cần thanh toán: <b>{policy.PremiumAmount:N0} VNĐ</b>.<br/>
                Vui lòng thực hiện thanh toán để duy trì quyền lợi bảo hiểm.";

                await _emailService.SendEmailAsync(policy.Email, "Nhắc phí bảo hiểm sắp đến hạn", content);
            }
        }
    }
}
