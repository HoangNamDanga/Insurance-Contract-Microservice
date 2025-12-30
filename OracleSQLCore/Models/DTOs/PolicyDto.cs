using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class PolicyDto
    {
        public int PolicyId { get; set; }          // POLICY_ID
        public string PolicyNumber { get; set; }   // POLICY_NUMBER
        public int CustomerId { get; set; }         // CUSTOMER_ID
        public int AgentId { get; set; }            // AGENT_ID
        public int InsTypeId { get; set; }          // INS_TYPE_ID
        public DateTime StartDate { get; set; }     // START_DATE
        public DateTime EndDate { get; set; }       // END_DATE
        public decimal PremiumAmount { get; set; } // PREMIUM_AMOUNT
        public string Status { get; set; }          // STATUS
    }
}
