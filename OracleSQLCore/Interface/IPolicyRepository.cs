using OracleSQLCore.Models.DTOs;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Interface
{
    public interface IPolicyRepository
    {
        // Tạo mới và trả về Event để bắn RabbitMQ
        Task<PolicyCreatedEvent> CreateAsync(PolicyDto policy);

        // Cập nhật và trả về Event để đồng bộ trạng thái sang Mongo
        Task<PolicyCreatedEvent> UpdateAsync(PolicyDto policy);

        // Xóa theo ID và trả về Event để Mongo xóa bản ghi tương ứng
        Task<PolicyCreatedEvent> DeleteAsync(int id);

        // Lấy thông tin gốc từ Oracle (dùng cho nội bộ hoặc đối soát)
        Task<PolicyDto> GetByIdAsync(int id);
        Task<List<PolicyDto>> GetAllAsync();

        // --- THÊM MỚI ---
        Task<PolicyChangedEvent> RenewAsync(RenewPolicyDto request);
        Task<PolicyChangedEvent> CancelAsync(CancelPolicyDto request);
    }
}
