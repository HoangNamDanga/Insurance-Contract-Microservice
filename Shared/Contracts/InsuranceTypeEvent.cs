using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class InsuranceTypeEvent
    {
        public int InsTypeId { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }
        public string Action { get; set; } // "CREATE", "UPDATE", "DELETE"
    }
}
