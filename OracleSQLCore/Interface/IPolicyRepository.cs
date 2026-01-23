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

        //API tinh hoa hong goi trigger
        // --- CÁC HÀM MỚI ---

        /// <summary>
        /// Xác nhận thanh toán tổng hợp: 
        /// 1. Cập nhật Payment -> Success
        /// 2. Cập nhật Policy -> Active
        /// 3. Tính hoa hồng cho Đại lý
        /// 4. Cập nhật Hạng đại lý (Hierarchy)
        /// </summary>
        /// <returns>Trả về Event chứa thông tin thay đổi để bắn vào RabbitMQ/Mongo</returns>
        Task<PaymentConfirmedEvent> ConfirmPaymentAsync(int paymentId);

        /// <summary>
        /// API lấy thông tin hoa hồng sau khi đã được trigger tính toán
        /// (Dùng để hiển thị hoặc đối soát ngay lập tức)
        /// </summary>
        Task<CommissionSyncDto> ConfirmAndGetCommissionAsync(int policyId);
    }
}
