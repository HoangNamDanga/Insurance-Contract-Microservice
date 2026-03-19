using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Services;
using OracleSQLCore.Services.Imp;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolicyBeneficiaryController : ControllerBase
    {
        private readonly IPolicyBeneficiaryService _policyBeneficiaryService;

        public PolicyBeneficiaryController(IPolicyBeneficiaryService policyBeneficiaryService)
        {
            _policyBeneficiaryService = policyBeneficiaryService;
        }

        // 1. Lấy danh sách Người thụ hưởng theo Policy ID
        [HttpGet("policy/{policyId}")]
        public async Task<IActionResult> GetByPolicyId(int policyId)
        {
            var beneficiary = await _policyBeneficiaryService.GetPolicyBeneficiaryByPolicyIdAsync(policyId);
            return Ok(beneficiary);
        }

        // 2. Lấy chi tiết 1 Người thụ hưởng theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var beneficiary = await _policyBeneficiaryService.GetPolicyBeneficiaryByIdAsync(id);
            if (beneficiary == null) return NotFound($"Không tìm thấy Người thụ hưởng với ID {id}");
            return Ok(beneficiary);
        }

        // 3. Tạo mới xe (Insert Oracle -> Publish Created Event)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PolicyBeneficiaryDto dto)
        {
            if (dto == null) return BadRequest("Dữ liệu không hợp lệ");

            try
            {
                var newId = await _policyBeneficiaryService.CreatePolicyBeneficiaryAsync(dto);

                // CẬP NHẬT ID MỚI VÀO DTO TRƯỚC KHI TRẢ VỀ
                dto.BeneficiaryId = newId;

                return CreatedAtAction(nameof(GetById), new { id = newId }, dto);
            }
            catch (System.Collections.Generic.KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // 4. Cập nhật Người thụ hưởng (Update Oracle -> Publish Updated Event)
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] PolicyBeneficiaryDto dto)
        {
            var updated = await _policyBeneficiaryService.UpdatePolicyBeneficiaryAsync(dto);
            if (!updated) return NotFound($"Không tìm thấy Người thụ hưởng ID {dto.BeneficiaryId} để cập nhật");

            return Ok(new { Message = "Cập nhật thành công và đang đồng bộ dữ liệu." });
        }

        // 5. Xóa Người thụ hưởng (Delete Oracle -> Publish Deleted Event)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _policyBeneficiaryService.DeletePolicyBeneficiaryAsync(id);
            if (!deleted) return NotFound($"Không tìm thấy Người thụ hưởng {id} để xóa");

            return Ok(new { Message = "Xóa thành công và đang đồng bộ dữ liệu." });
        }
    }
}
