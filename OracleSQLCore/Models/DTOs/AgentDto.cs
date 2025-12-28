using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class AgentDto
    {
        public int AgentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int? BranchId { get; set; }
        public DateTime? HireDate { get; set; }
    }
}
