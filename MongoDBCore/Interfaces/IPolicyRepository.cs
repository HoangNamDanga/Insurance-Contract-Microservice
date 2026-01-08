using MongoDBCore.Entities.Models;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Interfaces
{
    public interface IPolicyRepository
    {
        Task<List<PolicyDto>> GetAllAsync();
        Task<PolicyDto> GetByIdAsync(int id);
        Task UpsertAsync(PolicyDto policytDto);
        Task DeleteAsync(int id);

        //Nghiep vu moi'
        // Cap nhat khi gia han (Cần ngày mới, tiền mới, trạng thái mới)
        Task UpdateRenewStatusAsync(int id, DateTime newEndDate, decimal totalPremium, string notes);

        //Cap nhat khi huy? (Chỉ cần trạng thái và lý do)
        Task UpdateCancelStatusAsync(int id, string notes);

        // --- BỔ SUNG NGHIỆP VỤ SEARCH INDEXING ---
        // Tìm kiếm theo nhiều tiêu chí: Tên khách hàng, Trạng thái, và Khoảng ngày hết hạn
        Task<List<PolicyDto>> SearchAsync(string customerName, string status, DateTime? endDateFrom, DateTime? endDateTo);
    }
}
