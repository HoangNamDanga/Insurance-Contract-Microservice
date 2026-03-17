using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Interface
{
    public interface IPaymentRepository
    {
        // Tạo giao dịch mới (Mặc định Status = "Pending")
        Task<decimal> CreatePaymentAsync(PaymentDto dto);

        //Lấy thông tin chi tiết một giao dịch (Để kiểm tra trước khi update hoặc để Sync)
        Task<PaymentDto?> GetByIdAsync(decimal paymentId);

        //Lấy lịch sử thanh toán của một hợp đồng (Phục vụ tra cứu)
        Task<IEnumerable<PaymentDto>> GetByPolicyIdAsync(decimal policyId);

        // Nghiệp vụ quan trọng nhất. Cập nhật trạng thái (Success, Failed, Cancelled)
        //transactionId: Mã từ phía ngân hàng trả về sau khi thanh toán xong
        Task<bool> UpdateStatusAsync(decimal paymentId, string status, string transactionId = null);
    }
}
