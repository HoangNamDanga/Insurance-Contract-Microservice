using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;

namespace MongoDBCore.Controllers
{
    [ApiController]
    [Route("api/mongo/vehicles")] // Phân biệt với API Oracle bằng tiền tố 'mongo'
    public class VehicleQueryController : ControllerBase
    {
        private readonly IVehicleRepository _vehicleRepository;

        public VehicleQueryController(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        /// <summary>
        /// Lấy tất cả xe thuộc một Policy (Ưu tiên lấy từ Redis Cache)
        /// </summary>
        [HttpGet("policy/{policyId:int}")]
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetByPolicy(int policyId)
        {
            var vehicles = await _vehicleRepository.GetVehiclesByPolicyIdAsync(policyId);

            if (vehicles == null || !vehicles.Any())
                return NotFound($"Không tìm thấy danh sách xe cho Policy {policyId}");

            return Ok(vehicles);
        }

        /// <summary>
        /// Lấy chi tiết một xe cụ thể (Lấy từ Redis Cache)
        /// </summary>
        [HttpGet("{vehicleId:int}")]
        public async Task<ActionResult<VehicleDto>> GetById([FromQuery] int policyId, int vehicleId)
        {
            var vehicle = await _vehicleRepository.GetVehicleByIdAsync(policyId, vehicleId);

            if (vehicle == null)
                return NotFound($"Không tìm thấy xe với ID {vehicleId} trong Policy {policyId}");

            return Ok(vehicle);
        }

        /// <summary>
        /// Xóa xe (Xóa cả trong Mongo và dọn dẹp Cache)
        /// </summary>
        [HttpDelete("{vehicleId:int}")]
        public async Task<IActionResult> Delete(int policyId, int vehicleId)
        {
            var deleted = await _vehicleRepository.RemoveVehicleAsync(policyId, vehicleId);

            if (!deleted)
                return NotFound("Không thể xóa vì không tìm thấy dữ liệu.");

            return NoContent();
        }
    }
}