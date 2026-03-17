using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class PaymentDto
    {
        public decimal PaymentId { get; set; }
        public decimal PolicyId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentPeriod { get; set; }
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string? Method { get; set; }
        public string? Status { get; set; }
        public DateTime CreateAt { get; set; }

    }
}
