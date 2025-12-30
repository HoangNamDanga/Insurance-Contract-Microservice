using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    //Dùng để cập nhật trạng thái cho các nghiệp vụ phát sinh sau này (Renew, Cancel).
    public class PolicyChangedEvent
    {
        public int PolicyId { get; set; }
        public string PolicyNumber { get; set; }

        //thong tin nghiep vu thay doi
        public string ActionType { get; set; } // "CREATE", "UPDATE", "RENEW", "CANCEL"
        public string Status { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPremium { get; set; }
        public string LastNotes { get; set; }
        public DateTime ChangeDate { get; set; }
    }
}
