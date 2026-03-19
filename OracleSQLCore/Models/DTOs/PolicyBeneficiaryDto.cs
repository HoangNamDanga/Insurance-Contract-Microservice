using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class PolicyBeneficiaryDto
    {
        public int BeneficiaryId { get; set; }
        public int PolicyId { get; set; }
        public string FullName { get; set; }
        public string Relationship { get; set; }
        public string Phone { get; set; }

    }
}
