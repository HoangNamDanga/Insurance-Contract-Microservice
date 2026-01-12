using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public class PolicyPaymentDueEvent
    {
        public List<PolicyDueDto> DuePolicies { get; set; }
    }

    public class PolicyDueDto
    {
        public string PolicyNumber { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public decimal PremiumAmount { get; set; }
        public DateTime EndDate { get; set; }
    }
}
