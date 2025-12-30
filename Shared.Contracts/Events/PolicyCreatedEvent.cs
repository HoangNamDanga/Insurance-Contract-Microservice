using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{

    //Dùng để tạo mới bản ghi ở MongoDB với đầy đủ thông tin định danh (Customer, Agent, Insurance Type
    public class PolicyCreatedEvent
    {
        public int PolicyId { get; set; }
        public string PolicyNumber { get; set; }

        // Bổ sung các ID này
        public int CustomerId { get; set; }
        public int AgentId { get; set; }
        public int InsTypeId { get; set; }

        public string CustomerName { get; set; }
        public string AgentName { get; set; }
        public string InsTypeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PremiumAmount { get; set; }
        public string Status { get; set; }
        public string Action { get; set; } = "CREATE";
    }
}
