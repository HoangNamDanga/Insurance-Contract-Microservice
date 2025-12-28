using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using MongoDBCore.Interfaces;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Services;
using Polly;
using Polly.Caching;
using Polly.Retry;
using Shared.Contracts.Events;
namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly IAgentService _service;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IAgentRepository _repo;

        
        public AgentController(IAgentService service, IPublishEndpoint publishEndpoint, AsyncRetryPolicy retryPolicy,IAgentRepository repo)
        {
            _service = service;
            _publishEndpoint = publishEndpoint;
            // Định nghĩa Polly: Thử lại 3 lần nếu việc bắn tin vào RabbitMQ lỗi
            _retryPolicy = retryPolicy;
            _repo = repo;
        }


        //Write/Oracle
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AgentDto dto)
        {
            //Ghi vao Oracle
            var result = _service.Create(dto);

            Console.WriteLine($"DEBUG: Chuan bi gui Agent voi ID = {result.AgentId}");
            //2. Ban' su kien sang RabbitMQ de dong bo sang MongoDb (Read Side) - đồng bộ ở đây là đồng bộ dữ liệu
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _publishEndpoint.Publish(new AgentEvent
                {
                    AgentId = result.AgentId,
                    FullName = result.FullName,
                    Phone = result.Phone,
                    Email = result.Email,
                    Action = "CREATE"
                });
            });

            return Ok(new { Message = "Hoàng Nam đã tạo và gửi yêu cầu đồng bộ !", Data = result});
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] AgentDto dto)
        {
            _service.Update(dto);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _publishEndpoint.Publish(new AgentEvent
                {
                    AgentId = dto.AgentId,
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    Action = "UPDATE"
                });
            });

            return Ok("Đã cập nhật và gửi yêu cầu đồng bộ!");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(AgentDto dto)
        {
            _service.Delete(dto);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _publishEndpoint.Publish(new AgentEvent
                {
                    AgentId = dto.AgentId,
                    Action = "DELETE"
                });
            });

            return Ok("đã xóa và gửi yêu cầu đồng bộ !");
        }

        //Phan GET/MongoDb

        [HttpGet]
        public async Task<ActionResult<List<MongoDBCore.Entities.Models.AgentDto>>> GetAll()
        {
            var result = await _repo.GetAllAsync();
            if(result == null || result.Count == 0)
            {
                return NoContent();
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MongoDBCore.Entities.Models.AgentDto>> GetById(int id)
        {
            var result = await _repo.GetByIdAsync(id);
            if(result == null)
            {
                return NotFound(new { Message  = $"Không tìm thấy loại bảo hiểm với ID: {id}"});
            }

            return Ok(result);
        }
    }
}
