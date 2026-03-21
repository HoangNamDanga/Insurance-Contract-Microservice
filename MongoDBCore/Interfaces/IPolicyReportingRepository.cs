using MongoDBCore.Entities.Models.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Interfaces
{
    public interface IPolicyReportingRepository
    {
        // Báo cáo doanh thu theo tháng
        Task<List<RevenueReportDto>> GetMonthlyRevenueAsync(int year);

        // Báo cáo tỷ lệ bồi thường theo hãng xe
        Task<List<LossRatioReportDto>> GetLossRatioByBrandAsync();
    }
}
