using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Interfaces;
using Shared.Contracts.Events;
using OracleSQLCore.Services;
using Polly;
using Polly.Retry;
using OracleSQLCore.Models.DTOs;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InsuranceTypeController : ControllerBase
    {
        private readonly IInsuranceTypeService _service;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IInsuranceRepository _repo;
        public InsuranceTypeController(IInsuranceTypeService service, IPublishEndpoint publishEndpoint, AsyncRetryPolicy retryPolicy, IInsuranceRepository repo)
        {
            _repo = repo;
            _service = service;
            _publishEndpoint = publishEndpoint;
            // Định nghĩa Polly: Thử lại 3 lần nếu việc bắn tin vào RabbitMQ lỗi
            _retryPolicy = retryPolicy;
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
                await _publishEndpoint.Publish(new InsuranceTypeEvent
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
                await _publishEndpoint.Publish(new InsuranceTypeEvent
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
                await _publishEndpoint.Publish(new InsuranceTypeEvent
                {
                    InsTypeId = id,
                    Action = "DELETE"
                });
            });

            return Ok("Đã xóa và gửi yêu cầu đồng bộ!");
        }

        // GET: api/query/insurance-types
        [HttpGet]
        public async Task<ActionResult<List<InsuranceTypeDto>>> GetAll()
        {
            var results = await _repo.GetAllAsync();

            if (results == null || results.Count == 0)
            {
                return NoContent(); // Trả về 204 nếu trống
            }

            return Ok(results);
        }

        // GET: api/query/insurance-types/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<InsuranceTypeDto>> GetById(int id)
        {
            var result = await _repo.GetByIdAsync(id);

            if (result == null)
            {
                return NotFound(new { Message = $"Không tìm thấy loại bảo hiểm với ID: {id}" });
            }

            return Ok(result);
        }
    }
}
