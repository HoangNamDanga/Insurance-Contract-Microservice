using Dapper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Contracts.Events;
using System.Data;

namespace OracleSQLCore.Repositories.BackgroundServices
{
    public class PolicyWorker : BackgroundService
    {
        private readonly ILogger<PolicyWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public PolicyWorker(ILogger<PolicyWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        //Thiếu : Cần bổ sung Consumer cho nghiệp vụ này. pushlish bắn sang cho mongoDb consumer để gọi upsert cập nhật dữ liệu mới nhất bằng lệnh remove của mongoDb redis
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(">>> PolicyWorker ĐÃ KHỞI CHẠY: Chế độ quét định kỳ 00:00 hàng ngày <<<");

            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. Tính toán thời gian từ bây giờ đến 00:00 ngày mai
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1); // Lấy 00:00:00 của ngày tiếp theo
                var delay = nextRun - now;

                _logger.LogInformation("Lượt quét tiếp theo sẽ diễn ra sau: {0} giờ {1} phút (vào lúc {2:dd/MM/yyyy HH:mm:ss})",
                    (int)delay.TotalHours,
                    delay.Minutes,
                    nextRun);

                try
                {
                    // 2. Chờ đến đúng giờ G
                    await Task.Delay(delay, stoppingToken);

                    _logger.LogInformation(">>> KÍCH HOẠT CHU KỲ QUÉT ĐỊNH KỲ (00:00 AM) <<<");

                    // 3. Thực thi nghiệp vụ
                    await ExecuteInternal();

                    _logger.LogInformation(">>> HOÀN THÀNH CHU KỲ QUÉT. Nghỉ ngơi đợi lượt ngày mai...");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Worker đang dừng do ứng dụng tắt.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong vòng lặp Worker. Sẽ thử lại sau 30 phút.");
                    // Nếu lỗi (mất mạng, DB sập), đợi 30 phút rồi thử lại thay vì đợi đến tận ngày mai
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
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
                    var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                    _logger.LogInformation("--- BẮT ĐẦU CHU KỲ QUÉT {0:HH:mm:ss} ---", DateTime.Now);

                    // 1. Cập nhật EXPIRED
                    await dbConnection.ExecuteAsync(
                        "INSURANCE_USER.DHN_POLICY_PKG.AUTO_EXPIRE_POLICIES",
                        commandType: CommandType.StoredProcedure);

                    // 2. Lấy danh sách EXPIRED (Dùng Helper Class bên dưới)
                    var p1 = new OracleDynamicParameters();
                    p1.Add("p_cursor", OracleDbType.RefCursor, ParameterDirection.Output);

                    var expiredList = (await dbConnection.QueryAsync<PolicyEmailInfo>(
                        "INSURANCE_USER.DHN_POLICY_PKG.GET_EXPIRED_LIST",
                        p1,
                        commandType: CommandType.StoredProcedure)).ToList();

                    if (expiredList.Any())
                    {
                        await publishEndpoint.Publish(new PolicyExpiredEvent { ExpiredPolicies = expiredList });
                        _logger.LogInformation("=> [Hết hạn] Đã bắn tin cho {0} khách.", expiredList.Count);
                    }

                    // 3. Lấy danh sách NHẮC PHÍ
                    var p2 = new OracleDynamicParameters();
                    p2.Add("p_cursor", OracleDbType.RefCursor, ParameterDirection.Output);

                    var dueList = (await dbConnection.QueryAsync<PolicyDueDto>(
                        "INSURANCE_USER.DHN_POLICY_PKG.GET_PREMIUM_DUE_POLICIES",
                        p2,
                        commandType: CommandType.StoredProcedure)).ToList();

                    if (dueList.Any())
                    {
                        await publishEndpoint.Publish(new PolicyPaymentDueEvent { DuePolicies = dueList });
                        _logger.LogInformation("=> [Nhắc phí] Đã bắn tin cho {0} khách.", dueList.Count);
                    }

                    _logger.LogInformation("--- KẾT THÚC CHU KỲ THÀNH CÔNG ---");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiệp vụ trong ExecuteInternal.");
            }
        }
    }

    // HELPER CLASS ĐỂ FIX LỖI BINDING ORA-50028
    public class OracleDynamicParameters : SqlMapper.IDynamicParameters
    {
        private readonly DynamicParameters _dynamicParameters = new DynamicParameters();
        private readonly List<OracleParameter> _oracleParameters = new List<OracleParameter>();

        public void Add(string name, OracleDbType oracleDbType, ParameterDirection direction)
        {
            _oracleParameters.Add(new OracleParameter(name, oracleDbType, direction));
        }

        public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            ((SqlMapper.IDynamicParameters)_dynamicParameters).AddParameters(command, identity);
            if (command is OracleCommand oracleCommand)
            {
                oracleCommand.Parameters.AddRange(_oracleParameters.ToArray());
            }
        }
    }
}