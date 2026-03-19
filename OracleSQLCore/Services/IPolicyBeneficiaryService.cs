using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services
{
    public interface IPolicyBeneficiaryService
    {
        Task<int> CreatePolicyBeneficiaryAsync(PolicyBeneficiaryDto dto);
        Task<bool> UpdatePolicyBeneficiaryAsync(PolicyBeneficiaryDto dto);
        Task<bool> DeletePolicyBeneficiaryAsync(int beneficiaryId);
        Task<PolicyBeneficiaryDto> GetPolicyBeneficiaryByIdAsync(int beneficiaryId);
        Task<IEnumerable<PolicyBeneficiaryDto>> GetPolicyBeneficiaryByPolicyIdAsync(int policyId);
    }
}
