using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public class PaymentCreatedEvent
    {
        // Thay vì dùng PaymentDto, ta dùng các kiểu dữ liệu cơ bản
        public decimal PaymentId { get; set; }
        public decimal PolicyId { get; set; }
        public decimal Amount { get; set; }
        public string? Method { get; set; }
        public string? Status { get; set; }
        public string? PaymentPeriod { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
