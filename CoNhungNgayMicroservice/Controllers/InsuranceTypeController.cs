using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OracleSQLCore.Models;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Services;
using Polly;
using Polly.Retry;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InsuranceTypeController : ControllerBase
    {
        private readonly IInsuranceTypeService _service;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly AsyncRetryPolicy _retryPolicy;

        public InsuranceTypeController(IInsuranceTypeService service, IPublishEndpoint publishEndpoint, AsyncRetryPolicy retryPolicy)
        {
            _service = service;
            _publishEndpoint = publishEndpoint;
            // Định nghĩa Polly: Thử lại 3 lần nếu việc bắn tin vào RabbitMQ lỗi
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i)));
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InsuranceTypeDto dto)
        {
            // Ghi vào Oracle (Master Data)
            var result = _service.Create(dto);

            //2 . Ban su kien sang rabbitMq để đòng bộ sang MongoDb (Read Side) - đồng bộ ở đây là đồng bộ dữ liệu
            // còn luồng là bất đồng bộ
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _publishEndpoint.Publish(new MongoDBCore.Entities.Models.DTOs.InsuranceTypeEvent
                {
                    InsTypeId = result.InsTypeId,
                    TypeName = result.TypeName,
                    Description = result.Description,
                    Action = "CREATE"
                });
            });

            return Ok(new { Message = "Đã tạo và gửi yêu cầu đồng bộ!", Data = result });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] InsuranceTypeDto dto)
        {
            _service.Update(dto);
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _publishEndpoint.Publish(new MongoDBCore.Entities.Models.DTOs.InsuranceTypeEvent
                {
                    InsTypeId = dto.InsTypeId,
                    TypeName = dto.TypeName,
                    Description = dto.Description,
                    Action = "UPDATE"
                });
            });
            return Ok("Đã cập nhật và gửi yêu cầu đồng bộ!");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _service.Delete(id);
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _publishEndpoint.Publish(new MongoDBCore.Entities.Models.DTOs.InsuranceTypeEvent
                {
                    InsTypeId = id,
                    Action = "DELETE"
                });
            });

            return Ok("Đã xóa và gửi yêu cầu đồng bộ!");
        }
    }
}
