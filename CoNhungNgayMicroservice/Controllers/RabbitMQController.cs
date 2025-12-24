using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Entities.Models.DTOs;
using OracleSQLCore.Models;
using OracleSQLCore.Services;

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

        //Publish message (Oracle project)
        //flow Oracle → RabbitMQ → MongoDB
        public async Task<IActionResult> Create([FromBody] Customer customer, [FromServices] IPublishEndpoint publishEndpoint)
        {
            // 1. Lưu vào Oracle
            var id = await _customerService.CreateCustomer(customer);

            // 2. Thay vì gọi HTTP, ta bắn sự kiện vào RabbitMQ
            //Đây là thời điểm message nằm trong Queue
            await publishEndpoint.Publish(new CustomerSyncDto // Producer đã publish
            {
                CustomerId = id.ToString(),
                FullName = customer.FullName,
                Email = customer.Email
            });

            return Ok("Đã đưa vào hàng chờ đồng bộ!");
        }
    }
}
