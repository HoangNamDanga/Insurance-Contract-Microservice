using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Interface
{
    public interface IPolicyBeneficiaryRepository
    {
        Task<int> CreateAsync(PolicyBeneficiaryDto dto);
        Task<PolicyBeneficiaryDto> GetByIdAsync(int beneficiaryId);
        Task<IEnumerable<PolicyBeneficiaryDto>> GetByPolicyIdAsync(int policyId);

        Task<bool> UpdateAsync(PolicyBeneficiaryDto dto);
        Task<bool> DeleteAsync(int beneficiaryId);
    }
}
