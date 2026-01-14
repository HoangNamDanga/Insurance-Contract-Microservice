using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class CommissionSyncDto
    {
        public int PolicyId { get; set; }
        public string PolicyNumber { get; set; }
        public string CustomerName { get; set; }
        public int AgentId { get; set; }
        public string AgentName { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal CommissionAmount { get; set; }
        public string Status { get; set; }
        public string SyncDate { get; set; }
    }
}
