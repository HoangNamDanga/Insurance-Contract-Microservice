using MassTransit;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Repositories;
using Polly.Retry;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services.Imp
{
    public class PolicyBeneficiaryService : IPolicyBeneficiaryService
    {
        private readonly IPolicyBeneficiaryRepository _policyBeneficiaryRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly AsyncRetryPolicy _retryPolicy;

        public PolicyBeneficiaryService(
            IPolicyBeneficiaryRepository policyBeneficiaryRepository,
            IPublishEndpoint publishEndpoint,
            AsyncRetryPolicy retryPolicy)
        {
            _policyBeneficiaryRepository = policyBeneficiaryRepository;
            _publishEndpoint = publishEndpoint;
            _retryPolicy = retryPolicy;
        }

        public async Task<int> CreatePolicyBeneficiaryAsync(PolicyBeneficiaryDto dto)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                // 1. Lưu vào Oracle (Nghiệp vụ ghi là ưu tiên số 1)
                int newBeneficiaryId = await _policyBeneficiaryRepository.CreateAsync(dto);

                // 2. Bắn event lên RabbitMQ để bên Mongo tự lo phần hiển thị và cache
                await _publishEndpoint.Publish(new PolicyBeneficiaryCreatedEvent
                {
                    BeneficiaryId = newBeneficiaryId,
                    PolicyId = dto.PolicyId,
                    FullName = dto.FullName,
                    Relationship = dto.Relationship,
                    Phone = dto.Phone,
                    CreatedAt = DateTime.UtcNow,
                });

                return newBeneficiaryId;
            });
        }

        public async Task<bool> UpdatePolicyBeneficiaryAsync(PolicyBeneficiaryDto dto)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                // 1. Cập nhật Oracle
                bool updated = await _policyBeneficiaryRepository.UpdateAsync(dto);

                if (updated)
                {
                    // 2. Bắn Event Update để đồng bộ sang MongoDB
                    // Lưu ý: Bạn nên tạo thêm VehicleUpdatedEvent trong Shared.Contracts
                    await _publishEndpoint.Publish(new PolicyBeneficiaryUpdatedEvent
                    {
                        BeneficiaryId = dto.BeneficiaryId,
                        PolicyId = dto.PolicyId,
                        FullName = dto.FullName,
                        Relationship = dto.Relationship,
                        Phone = dto.Phone,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                return updated;
            });
        }



        public async Task<bool> DeletePolicyBeneficiaryAsync(int beneficiaryId)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                // Lấy thông tin xe trước khi xóa để lấy PolicyId gửi đi
                var beneficiary = await _policyBeneficiaryRepository.GetByIdAsync(beneficiaryId);
                if (beneficiary == null) return false;

                bool deleted = await _policyBeneficiaryRepository.DeleteAsync(beneficiaryId);
                if (deleted)
                {
                    // 3. Bắn Event xóa để bên Mongo thực hiện $pull khỏi mảng
                    await _publishEndpoint.Publish(new PolicyBeneficiaryDeletedEvent
                    {
                        BeneficiaryId = beneficiaryId,
                        PolicyId = beneficiary.PolicyId
                    });
                }

                return deleted;
            });
        }

        public async Task<PolicyBeneficiaryDto> GetPolicyBeneficiaryByIdAsync(int beneficiaryId)
        {
            return await _policyBeneficiaryRepository.GetByIdAsync(beneficiaryId);
        }

        public async Task<IEnumerable<PolicyBeneficiaryDto>> GetPolicyBeneficiaryByPolicyIdAsync(int policyId)
        {
            return await _policyBeneficiaryRepository.GetByPolicyIdAsync(policyId);
        }


    }
}
