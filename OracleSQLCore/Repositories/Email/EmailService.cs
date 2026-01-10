using Microsoft.Extensions.Configuration;
using MimeKit;
using Shared.Contracts.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using MailKit.Net.Smtp;          // THÊM dòng này (Thư viện MailKit)
using MailKit.Security;          // THÊM dòng này (Để dùng SecureSocketOptions)
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Repositories.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;


        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["EmailSettings:SenderName"], _config["EmailSettings:SenderEmail"]));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Lấy thông tin từ config (Docker hoặc appsettings)
            var server = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:SmtpPort"]);

            // Kết nối tới MailHog (không dùng SSL vì là môi trường test)
            await client.ConnectAsync(server, port, MailKit.Security.SecureSocketOptions.None);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
