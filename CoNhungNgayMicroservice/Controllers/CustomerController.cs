using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OracleSQLCore.Models;
using OracleSQLCore.Services;
using System.Net.Http;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //Khi nhận lệnh POST từ Swagger, nó thực hiện lưu vào Oracle và "nhấc máy" gọi điện (HTTP Request) sang phía Mongo
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IHttpClientFactory _httpClientFactory;
        public CustomerController(ICustomerService customerService, IHttpClientFactory httpClientFactory)
        {
            _customerService = customerService; // Service được DI vào Controller
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Create([FromBody] Customer customer)
        {
            var id = await _customerService.CreateCustomer(customer);

            var syncData = new MongoDBCore.Entities.Models.DTOs.CustomerSyncDto
            {
                CustomerId = id.ToString(),
                FullName = customer.FullName,
                Email = customer.Email
            };

            var client = _httpClientFactory.CreateClient("MongoSyncClient"); // polly

            try
            {
                var response = await client.PostAsJsonAsync(
                    "http://api:8080/api/CustomerMongo/sync-from-oracle",
                    syncData);

                if (response.IsSuccessStatusCode)
                {
                    return Ok("Đồng bộ thành công!");
                }

                var errorBody = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Lỗi đồng bộ: {errorBody}");
            }
            catch (HttpRequestException ex)
            {
                // Xảy ra khi Polly đã retry xong mà vẫn fail
                return StatusCode(503, $"Mongo service không phản hồi: {ex.Message}");
            }
        }



        // GET: api/Customer
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> Get()
        {
            var customers = await _customerService.GetAllCustomers();
            return Ok(customers);
        }

        // GET: api/Customer/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> Get(int id)
        {
            var customer = await _customerService.GetCustomerDetails(id);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        // POST: api/Customer
        [HttpPost]
        public async Task<ActionResult<int>> Post([FromBody] Customer customer)
        {
            try
            {
                var newId = await _customerService.CreateCustomer(customer);
                return CreatedAtAction(nameof(Get), new { id = newId }, newId);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Customer/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Customer customer)
        {
            if (id != customer.CustomerId) return BadRequest();

            var success = await _customerService.UpdateCustomerInfo(customer);
            if (!success) return NotFound();

            return NoContent();
        }

        // DELETE: api/Customer/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _customerService.RemoveCustomer(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
