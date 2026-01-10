using Dapper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Repositories.BackgroundServices
{
    public class PolicyWorker : BackgroundService
    {
        private readonly ILogger<PolicyWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory; // Chỉ giữ lại cái này

        public PolicyWorker(ILogger<PolicyWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(">>> PolicyWorker Đã khởi tạo và đang chờ lịch quét....");

            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. Tính toán thời gian nghỉ đến 00:00 ngày mai
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1); // Mặc định chạy vào 00:00 sáng mai
                var delay = nextRun - now;

                _logger.LogInformation("Lịch quét tiếp theo sẽ chạy sau {0:0.##} giờ (lúc 00:00)", delay.TotalHours);

                try
                {
                    // 2. Chờ đến đúng giờ mới chạy
                    // Nếu bạn muốn chạy NGAY LẬP TỨC khi khởi động lần đầu, hãy bỏ qua dòng delay này ở lần lặp đầu tiên.
                    await Task.Delay(delay, stoppingToken);

                    // 3. Thực thi nghiệp vụ
                    await ExecuteInternal();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi không mong muốn trong vòng lặp Worker.");
                    // Nếu lỗi, đợi 1 phút rồi thử lại để tránh treo vòng lặp vô tận
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        public async Task ExecuteInternal()
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbConnection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
                    // Lấy PublishEndpoint từ scope để đảm bảo an toàn thread
                    var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                    _logger.LogInformation("--- BẮT ĐẦU CHU KỲ QUÉT HỢP ĐỒNG {0} ---", DateTime.Now);

                    // 1. Chạy Procedure cập nhật trạng thái
                    await dbConnection.ExecuteAsync(
                        "INSURANCE_USER.DHN_POLICY_PKG.AUTO_EXPIRE_POLICIES",
                        commandType: CommandType.StoredProcedure);

                    // 2. Query lấy thông tin những ông VỪA bị chuyển sang EXPIRED
                    var sqlGetExpired = @"
                        SELECT p.POLICY_NUMBER as PolicyNumber, 
                               c.EMAIL as CustomerEmail, 
                               c.FULL_NAME as CustomerName, 
                               p.END_DATE as EndDate
                        FROM INSURANCE_USER.DHN_POLICY p
                        JOIN INSURANCE_USER.DHN_CUSTOMER c ON p.CUSTOMER_ID = c.CUSTOMER_ID
                        WHERE p.STATUS = 'EXPIRED' 
                          AND p.END_DATE <= SYSDATE 
                          AND p.END_DATE > SYSDATE - 1";

                    var expiredList = (await dbConnection.QueryAsync<PolicyEmailInfo>(sqlGetExpired)).ToList();

                    if (expiredList.Any())
                    {
                        // 3. Bắn tin nhắn vào RabbitMQ
                        await publishEndpoint.Publish(new PolicyExpiredEvent
                        {
                            ExpiredPolicies = expiredList.ToList()
                        });

                        _logger.LogInformation("=> Đã gửi yêu cầu email cho {0} khách hàng.", expiredList.Count);
                    }

                    _logger.LogInformation("--- THỰC THI ORACLE THÀNH CÔNG ---");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thực thi nghiệp vụ quét Oracle.");
            }
        }
    }
}
