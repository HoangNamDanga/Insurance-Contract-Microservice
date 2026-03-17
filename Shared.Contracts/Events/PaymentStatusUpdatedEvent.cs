using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public class PaymentStatusUpdatedEvent
    {
        public decimal PaymentId { get; set; }
        public string NewStatus { get; set; }
        public string TransactionId { get; set; }

        // Thay vì gửi nguyên PaymentDto, ta gửi các trường cần cập nhật nhất
        public decimal PolicyId { get; set; }
        public decimal Amount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
