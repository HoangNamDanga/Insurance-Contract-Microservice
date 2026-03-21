using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models.Report
{
    public class LossRatioReportDto
    {
        public string VehicleBrand { get; set; }
        public decimal TotalPremium { get; set; }
        public decimal TotalClaimPaid { get; set; }
        public double LossRatioPercent { get; set; }
    }
}
