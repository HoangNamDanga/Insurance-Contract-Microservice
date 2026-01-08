using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    //Dto nghiep vu Gia Han
    public class RenewPolicyDto
    {
        public int PolicyId { get; set; }
        public DateTime NewEndDate { get; set; } 
        public decimal AdditionalPremium { get; set; } 
        public string Notes { get; set; } = string.Empty;
    }
}
