using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class ClaimCreateDto
    {
        public int PolicyId { get; set; }
        public DateTime ClaimDate { get; set; }
        public decimal AmountClaimed { get; set; }
        public string Description { get; set; }
        // Mặc định Status khi nộp sẽ là 'Pending'
    }

    public class ClaimSyncDto
    {
        public int ClaimId { get; set; }
        public int PolicyId { get; set; }
        public string PolicyNumber { get; set; } // Lấy từ bảng DHN_POLICY
        public string CustomerName { get; set; } // Lấy từ bảng DHN_CUSTOMER
        public DateTime ClaimDate { get; set; }
        public decimal AmountClaimed { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime SyncDate { get; set; } = DateTime.Now;
    }
}
