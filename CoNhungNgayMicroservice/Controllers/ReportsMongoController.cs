using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Interfaces;
using MongoDBCore.Entities.Models.Report;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDBCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IPolicyReportingRepository _reportingRepository;

        public ReportsController(IPolicyReportingRepository reportingRepository)
        {
            _reportingRepository = reportingRepository;
        }

        /// <summary>
        /// Lấy báo cáo doanh thu theo tháng của một năm cụ thể
        /// GET: api/reports/revenue/2026
        /// </summary>
        [HttpGet("revenue/{year}")]
        public async Task<ActionResult<List<RevenueReportDto>>> GetMonthlyRevenue(int year)
        {
            var data = await _reportingRepository.GetMonthlyRevenueAsync(year);
            if (data == null || data.Count == 0)
            {
                return NotFound(new { message = $"Không có dữ liệu doanh thu cho năm {year}" });
            }
            return Ok(data);
        }

        /// <summary>
        /// Lấy báo cáo tỷ lệ bồi thường theo hãng xe
        /// GET: api/reports/loss-ratio
        /// </summary>
        [HttpGet("loss-ratio")]
        public async Task<ActionResult<List<LossRatioReportDto>>> GetLossRatioByBrand()
        {
            var data = await _reportingRepository.GetLossRatioByBrandAsync();
            if (data == null || data.Count == 0)
            {
                return NotFound(new { message = "Không có dữ liệu bồi thường để hiển thị" });
            }
            return Ok(data);
        }
    }
}