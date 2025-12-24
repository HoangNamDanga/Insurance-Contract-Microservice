using MassTransit;
using MongoDBCore.Entities.Models.DTOs;
using MongoDBCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories
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
            // Gọi logic lưu vào MongoDB
            await _mongoService.CreateUser(data);
            Console.WriteLine($"[Đã đồng bộ]: {data.FullName}");
        }
    }
}
