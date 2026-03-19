using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public class PolicyBeneficiaryDeletedEvent
    {
        public int BeneficiaryId { get; set; }
        public int PolicyId { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
