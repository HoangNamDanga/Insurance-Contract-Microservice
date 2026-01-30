using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class ClaimUpdateStatusRequest
    {
        public int ClaimId { get; set; }
        public string Status { get; set; } // APPROVED, REJECTED
        public decimal? AmountApproved { get; set; }
        public string Description { get; set; }
    }
}
