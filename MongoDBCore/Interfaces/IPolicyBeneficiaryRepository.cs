using MongoDBCore.Entities.Models;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Interfaces
{
    public interface IPolicyBeneficiaryRepository
    {

        // 1. Upsert: Thêm mới hoặc cập nhật thông tin xe trong mảng của Policy
        // Nếu xe đã tồn tại (khớp VehicleId) thì cập nhật, nếu chưa thì đẩy ($push) vào mảng
        Task<bool> UpsertPolicyBeneficiaryAsync(int policyId, PolicyBeneficiaryCreatedEvent policyBeneficiaryeData);

        // 2. Remove: Xóa xe khỏi mảng của Policy
        Task<bool> RemovePolicyBeneficiaryAsync(int policyId, int beneficiaryeId);

        // 3. GetById: Tìm 1 xe cụ thể nằm trong mảng của một Policy
        // MongoDB sẽ trả về Object Vehicle sau khi lọc từ mảng
        Task<PolicyBeneficiaryDto> GetPolicyBeneficiaryByIdAsync(int policyId, int beneficiaryeId);

        // 4. GetByPolicyId: Lấy toàn bộ danh sách xe thuộc về 1 Policy
        Task<IEnumerable<PolicyBeneficiaryDto>> GetPolicyBeneficiaryByPolicyIdAsync(int policyId);


    }
}
