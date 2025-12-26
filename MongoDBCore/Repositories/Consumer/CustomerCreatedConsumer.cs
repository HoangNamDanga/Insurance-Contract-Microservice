using MassTransit;
using MongoDBCore.Entities.Models.DTOs;
using MongoDBCore.Services;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories.Consumer
{

    //Cấu hình MassTransit sẽ đăng ký consumer này và endpoint để lắng nghe queue: trong file program.cs
    public class CustomerCreatedConsumer : IConsumer<CustomerSyncDto>
    {
        private readonly ICustomerService _mongoService;

        public CustomerCreatedConsumer(ICustomerService mongoService)
        {
            _mongoService = mongoService;
        }

        //consumer nhận message từ RabbitMQ và xử lý logic lưu vào MongoDB.
        public async Task Consume(ConsumeContext<CustomerSyncDto> context)
        {
            var data = context.Message;

            // 1. Định nghĩa chính sách Retry
            // Thử lại 3 lần, mỗi lần cách nhau 2 giây nếu có lỗi xảy ra
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        // Logic này chạy mỗi khi bị lỗi và chuẩn bị thử lại
                        Console.WriteLine($"[Lỗi]: {exception.Message}. Đang thử lại lần {retryCount}...");
                    });

            // 2. Thực thi logic lưu vào MongoDB bên trong lớp bảo vệ của Polly
            await retryPolicy.ExecuteAsync(async () =>
            {
                // Gọi logic lưu vào MongoDB
                await _mongoService.CreateUser(data);
                Console.WriteLine($"[Đã đồng bộ thành công]: {data.FullName}");
            });
        }
    }
}
