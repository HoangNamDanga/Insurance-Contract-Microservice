using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class CancelPolicyDto
    {
       public int PolicyId { get; set; }
        public string Reason { get; set; }
    }
}
