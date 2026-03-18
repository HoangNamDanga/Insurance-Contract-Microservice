using Microsoft.AspNetCore.Mvc;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Services;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        // 1. Lấy danh sách xe theo Policy ID
        [HttpGet("policy/{policyId}")]
        public async Task<IActionResult> GetByPolicyId(int policyId)
        {
            var vehicles = await _vehicleService.GetVehiclesByPolicyIdAsync(policyId);
            return Ok(vehicles);
        }

        // 2. Lấy chi tiết 1 xe theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null) return NotFound($"Không tìm thấy xe với ID {id}");
            return Ok(vehicle);
        }

        // 3. Tạo mới xe (Insert Oracle -> Publish Created Event)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VehicleDto dto)
        {
            if (dto == null) return BadRequest("Dữ liệu không hợp lệ");

            try
            {
                var newId = await _vehicleService.CreateVehicleAsync(dto);

                // CẬP NHẬT ID MỚI VÀO DTO TRƯỚC KHI TRẢ VỀ
                dto.VehicleId = newId;

                return CreatedAtAction(nameof(GetById), new { id = newId }, dto);
            }
            catch (System.Collections.Generic.KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // 4. Cập nhật xe (Update Oracle -> Publish Updated Event)
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] VehicleDto dto)
        {
            var updated = await _vehicleService.UpdateVehicleAsync(dto);
            if (!updated) return NotFound($"Không tìm thấy xe ID {dto.VehicleId} để cập nhật");

            return Ok(new { Message = "Cập nhật thành công và đang đồng bộ dữ liệu." });
        }

        // 5. Xóa xe (Delete Oracle -> Publish Deleted Event)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _vehicleService.DeleteVehicleAsync(id);
            if (!deleted) return NotFound($"Không tìm thấy xe ID {id} để xóa");

            return Ok(new { Message = "Xóa thành công và đang đồng bộ dữ liệu." });
        }
    }
}