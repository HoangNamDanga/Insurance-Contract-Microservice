using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class ClaimCancelRequest
    {
        public int ClaimId { get; set; }
        public string Reason { get; set; }
    }
}
