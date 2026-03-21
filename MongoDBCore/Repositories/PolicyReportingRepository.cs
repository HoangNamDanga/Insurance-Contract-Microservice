using MongoDB.Driver;
using MongoDBCore.Entities.Models;
using MongoDBCore.Entities.Models.Report;
using MongoDBCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories
{
    public class PolicyReportingRepository : IPolicyReportingRepository
    {
        private readonly IMongoCollection<PolicyDto> _policies;

        public PolicyReportingRepository(IMongoDatabase database)
        {
            // Kết nối tới Collection "Policies" (Nơi lưu trữ PolicyMongoDto)
            _policies = database.GetCollection<PolicyDto>("Policy");
        }

        // 1. BÁO CÁO TỶ LỆ BỒI THƯỜNG THEO HÃNG XE
        public async Task<List<LossRatioReportDto>> GetLossRatioByBrandAsync()
        {
            var report = await _policies.Aggregate()
                // Bước 1: Chỉ lấy các hợp đồng đang hoạt động
                .Match(p => p.Status == "ACTIVE")
                // Bước 2: Gom nhóm theo Hãng xe (Brand)
                .Group(p => p.Vehicle.Brand, g => new
                {
                    Brand = g.Key, // Group theo cái gì → thì Key chính là cái đó, ở đây là Vehicle.Brand
                    TotalPremium = g.Sum(p => p.PremiumAmount),
                    // Tính tổng tiền bồi thường từ mảng Claims lồng bên trong
                    TotalClaimPaid = g.Sum(p => p.Claims
                                        .Where(c => c.Status == "Approved")
                                        .Sum(c => c.AmountApproved))
                })
                // Bước 3: Đổ dữ liệu vào DTO và tính toán tỷ lệ %
                .Project(r => new LossRatioReportDto
                {
                    VehicleBrand = r.Brand,
                    TotalPremium = r.TotalPremium,
                    TotalClaimPaid = r.TotalClaimPaid,
                    LossRatioPercent = r.TotalPremium > 0
                        ? (double)(r.TotalClaimPaid / r.TotalPremium) * 100
                        : 0
                })
                // Bước 4: Sắp xếp theo hãng xe có tỷ lệ bồi thường cao nhất lên đầu
                .SortByDescending(r => r.LossRatioPercent)
                .ToListAsync();

            return report;
        }

        // 2. BÁO CÁO DOANH THU THEO THÁNG
        // Lưu ý: Giả sử trong PolicyMongoDto bạn có thêm trường CreatedDate hoặc PaymentDate
        // Ở đây tôi sẽ demo dựa trên logic Group theo dữ liệu thời gian
        public async Task<List<RevenueReportDto>> GetMonthlyRevenueAsync(int year)
        {
            // Trong thực tế, bạn cần lưu thêm trường DateTime trong PolicyMongoDto 
            // để trích xuất tháng. Ví dụ: p.CreatedDate

            var pipeline = _policies.Aggregate()
                .Match(p => p.Status == "ACTIVE")
                .Group(
                    p => new { p.StartDate.Year, p.StartDate.Month }, // “Lấy các record ACTIVE → rồi nhóm chúng lại theo Year và Month của StartDate”
                    g => new RevenueReportDto // g => → xử lý dữ liệu trong từng nhóm
                    {
                        Month = g.Key.Month, // các cột trả ra
                        TotalRevenue = g.Sum(x => x.PremiumAmount),
                        TransactionCount = g.Count()
                    }
                )
                .SortBy(r => r.Month);
            return await pipeline.ToListAsync();
        }
    }
}