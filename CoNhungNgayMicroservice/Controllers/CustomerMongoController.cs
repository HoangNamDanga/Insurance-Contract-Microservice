using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Entities.Models;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //CustomerMongoController (MongoDB - Người nhận): Nó "ngồi chờ" yêu cầu từ CustomerController tại endpoint sync-from-oracle để lưu vào MongoDB.
    public class CustomerMongoController : ControllerBase
    {

        private readonly MongoDBCore.Services.ICustomerService _customerService;

        public CustomerMongoController(MongoDBCore.Services.ICustomerService customerService)
        {
            _customerService = customerService; // Service được DI vào Controller
        }


        //Chờ yêu cầu từ CustomerController
        //Endpoint này đóng vai trò là "Service B", chờ Service A gọi tới để lưu dữ liệu đồng bộ
        [HttpPost("sync-from-oracle")]
        public async Task<IActionResult> SyncCustomer([FromBody] CustomerSyncDto customer) //Đồng bộ hóa trạng thái dữ liệu
        {
            // Sử dụng Service từ MongoDBCore để lưu vào MongoDB
            await _customerService.CreateUser(customer);
            return Ok(new { message = "Data synced to MongoDB successfully" });
        }
    }
}
