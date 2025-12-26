using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Entities.Models.DTOs;
using OracleSQLCore.Models;
using OracleSQLCore.Services;
using Polly;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RabbitMQController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IHttpClientFactory _httpClientFactory;
        public RabbitMQController(ICustomerService customerService, IHttpClientFactory httpClientFactory)
        {
            _customerService = customerService; // Service được DI vào Controller
            _httpClientFactory = httpClientFactory;
        }


        [HttpPost("sync")]
        public async Task<IActionResult> Create([FromBody] Customer customer, [FromServices] IPublishEndpoint publishEndpoint)
        {
            // 1. Lưu vào Oracle (Database nội bộ)
            var id = await _customerService.CreateCustomer(customer);

            // Định nghĩa Policy (Thường sẽ khai báo tập trung ở Program.cs hoặc biến static)
            var retryPolicy = Policy
                .Handle<Exception>() // Bắt các lỗi kết nối RabbitMQ
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2));

            // 2. Polly bao bọc hành động Publish
            await retryPolicy.ExecuteAsync(async () =>
            {
                await publishEndpoint.Publish(new CustomerSyncDto
                {
                    CustomerId = id.ToString(),
                    FullName = customer.FullName,
                    Email = customer.Email
                });
            });

            return Ok("Đã đưa vào hàng chờ đồng bộ!");
        }
    }
}
