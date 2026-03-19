using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolicyBeneficiaryQueryControllerr : ControllerBase
    {
        private readonly IPolicyBeneficiaryRepository _policyBeneficiarRepository;

        public PolicyBeneficiaryQueryControllerr(IPolicyBeneficiaryRepository policyBeneficiarRepository)
        {
            _policyBeneficiarRepository = policyBeneficiarRepository;
        }

        /// <summary>
        /// Lấy tất cả Người thụ hưởng thuộc một Policy (Ưu tiên lấy từ Redis Cache)
        /// </summary>
        [HttpGet("policy/{policyId:int}")]
        public async Task<ActionResult<IEnumerable<PolicyBeneficiaryDto>>> GetByPolicy(int policyId)
        {
            var policyBeneficiar = await _policyBeneficiarRepository.GetPolicyBeneficiaryByPolicyIdAsync(policyId);

            if (policyBeneficiar == null || !policyBeneficiar.Any())
                return NotFound($"Không tìm thấy danh sách Người thụ hưởng cho Policy {policyId}");

            return Ok(policyBeneficiar);
        }

        /// <summary>
        /// Lấy chi tiết một Người thụ hưởnge cụ thể (Lấy từ Redis Cache)
        /// </summary>
        [HttpGet("{beneficiar:int}")]
        public async Task<ActionResult<PolicyBeneficiaryDto>> GetById([FromQuery] int policyId, int beneficiarId)
        {
            var beneficiar = await _policyBeneficiarRepository.GetPolicyBeneficiaryByIdAsync(policyId, beneficiarId);

            if (beneficiar == null)
                return NotFound($"Không tìm thấy Người thụ hưởng với ID {beneficiar} trong Policy {policyId}");

            return Ok(beneficiar);
        }

        /// <summary>
        /// Xóa xe (Xóa cả trong Mongo và dọn dẹp Cache)
        /// </summary>
        [HttpDelete("{beneficiarId:int}")]
        public async Task<IActionResult> Delete(int policyId, int beneficiarId)
        {
            var deleted = await _policyBeneficiarRepository.RemovePolicyBeneficiaryAsync(policyId, beneficiarId);

            if (!deleted)
                return NotFound("Không thể xóa vì không tìm thấy dữ liệu.");

            return NoContent();
        }
    }
}
