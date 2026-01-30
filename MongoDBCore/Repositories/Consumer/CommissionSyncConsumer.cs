using MassTransit;
using Microsoft.AspNetCore.SignalR;
using MongoDBCore.Entities.Models;
using MongoDBCore.Hubs;
using MongoDBCore.Interfaces;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MongoDBCore.Repositories.Consumer
{
    public class CommissionSyncConsumer : IConsumer<PaymentConfirmedEvent>
    {
        private readonly ICommissionRepository _mongoRepo;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CommissionSyncConsumer(ICommissionRepository mongoRepo, IHubContext<NotificationHub> hubContext)
        {
            _mongoRepo = mongoRepo;
            _hubContext = hubContext;
        }

        //Nghiệp vụ tin
        public async Task Consume(ConsumeContext<PaymentConfirmedEvent> context)
        {
            var data = context.Message;

            // GHI LOG RA CONSOLE ĐỂ KIỂM TRA TRONG DOCKER LOGS
            Console.WriteLine($"[CRITICAL_LOG] Consumer đã nhận được PaymentId: {data.PaymentId}");
            // Chuyển đổi dữ liệu sang Model AgentCommissionMongo của bạn
            var mongoData = new CommissionMongo
            {
                PaymentId = data.PaymentId, // Lấy từ Event
                PolicyId = data.PolicyId,
                PolicyNumber = data.PolicyNumber,
                CustomerName = data.CustomerName,
                AgentId = data.AgentId,
                AgentName = data.AgentName,
                NewAgentLevel = data.NewAgentLevel,
                TotalPayment = data.TotalPayment,
                CommissionAmount = data.CommissionAmount,
                Status = "SUCCESS",
                ProcessedAt = data.ProcessedAt
            };


            await _mongoRepo.UpsertCommissionAsync(mongoData);
            //Khi có tin từ RabbitMQ -> Ghi Mongo xong -> Cầm cái "loa" IHubContext hét lên -> Client nhận được ngay lập tức
            //2. Phát tin REAL-TIME
            // Gửi thông báo đến tất cả người dùng hoặc 1 User cụ thể (theo AgentId)
            await _hubContext.Clients.All.SendAsync("ReceivePaymentUpdate", new
            {
                Message = $"Giao Dịch {data.PaymentId} thành công !",
                NewRank = data.NewAgentLevel,
                Amount = data.CommissionAmount
            });

            Console.WriteLine($"[REALTIME_LOG] đã phát thông báo cho giao dịch {data.PaymentId}");

            // Bỏ qua mọi logic lưu trữ, cứ có tin là phát loa luôn!
            //await _hubContext.Clients.All.SendAsync("ReceivePaymentUpdate", new
            //{
            //    Message = "TEST THÔI - WebSocket ĐANG CHẠY!",
            //    NewRank = "Pro",
            //    Amount = 999999
            //});

            //Console.WriteLine("[TEST] Đã ép phát tin qua WebSocket!");
        }
        //nếu chạy trên docker: Nếu frontend nằm ở một domain khác (hoặc port khác), phải cấu hình CORS trong Program.cs để cho phép trình duyệt kết nối vào Hub này, nếu không kết nối Real-time sẽ bị chặn (Refused)

    }
}
