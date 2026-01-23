using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public class PaymentConfirmedEvent
    {
        // Thông tin định danh giao dịch
        public int PaymentId { get; set; }
        public int PolicyId { get; set; }
        public string? PolicyNumber { get; set; } // THÊM: Số hợp đồng (ví dụ: POL-2024-001)

        // Thông tin khách hàng (Để hiển thị trong báo cáo hoa hồng của Đại lý)
        public string? CustomerName { get; set; } // THÊM: Tên khách hàng thanh toán

        // Thông tin trạng thái và hạng
        public string? NewPolicyStatus { get; set; }
        public int AgentId { get; set; }
        public string? AgentName { get; set; }    // THÊM: Tên đại lý thụ hưởng hoa hồng
        public string? NewAgentLevel { get; set; }

        // Thông tin tài chính
        public decimal TotalPayment { get; set; }  // THÊM: Tổng số tiền phí bảo hiểm đã đóng
        public decimal CommissionAmount { get; set; }

        // Thời gian xử lý
        public DateTime ProcessedAt { get; set; }
    }
}
