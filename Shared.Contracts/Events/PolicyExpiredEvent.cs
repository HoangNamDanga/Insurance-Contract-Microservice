using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public class PolicyExpiredEvent
    {
        // Chứa danh sách thông tin khách hàng cần gửi mail
        public List<PolicyEmailInfo> ExpiredPolicies { get; set; } = new();
    }

    public class PolicyEmailInfo
    {
        public string PolicyNumber { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerName { get; set; }
        public DateTime EndDate { get; set; }
    }
}
