using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services.Imp
{
    public class ClaimService : IClaimService
    {
        private readonly IClaimRepository _claimRepo;
        private readonly IHttpClientFactory _httpClientFactory;

        public ClaimService(IClaimRepository claimRepo, IHttpClientFactory httpClientFactory)
        {
            _claimRepo = claimRepo;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(bool IsSuccess, string Message)> CancelClaimAsync(int claimId, string reason)
        {
            try
            {
                // 1. Gọi Repository để chạy Procedure PRC_CANCEL_CLAIM trong Oracle
                bool isCancelled = await _claimRepo.CancelClaimAsync(claimId, reason);

                if (isCancelled)
                {
                    // 2. Lấy dữ liệu mới nhất (vừa chuyển sang CANCELLED) để đồng bộ
                    var syncDto = await _claimRepo.GetClaimForSyncAsync(claimId);

                    // 3. Đồng bộ sang MongoDB Service qua HTTP
                    // Điều này đảm bảo bên Mongo trạng thái cũng chuyển sang 'CANCELLED'
                    await SyncToMongoAsync(syncDto);

                    return (true, "Hủy yêu cầu bồi thường và đồng bộ thành công.");
                }

                return (false, "Không thể hủy hồ sơ. Có thể hồ sơ không ở trạng thái Chờ (PENDING).");
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                return (false, $"Lỗi hệ thống khi hủy hồ sơ: {ex.Message}");
            }
        }


        //Thủ tục Duyệt hoặc Từ chối bồi thường
        public async Task<(bool IsSuccess, string Message)> ProcessClaimStatusAsync(int claimId, string status, decimal? amountApproved, string note)
        {
            try
            {
                // 1. Cập nhật Oracle thông qua Repository
                bool isUpdated = await _claimRepo.UpdateClaimStatusAsync(claimId, status, amountApproved, note);

                if (isUpdated)
                {
                    // 2. Lấy dữ liệu để đồng bộ (Lấy data phẳng từ Oracle)
                    var syncDto = await _claimRepo.GetClaimForSyncAsync(claimId);

                    // 3. Đồng bộ HTTP sang MongoDB Service
                    await SyncToMongoAsync(syncDto);

                    return (true, "Cập nhật trạng thái và đồng bộ thành công.");
                }
                return (false, "Không thể cập nhật trạng thái hồ sơ.");
            }
            catch (Exception ex)
            {
                // Trả về thông báo lỗi nghiệp vụ từ Oracle (ví dụ: "Số tiền duyệt quá lớn")
                return (false, ex.Message);
            }
        }

        #region Nghiệp vụ Quản lý Bồi thường (Claims Management) Validate trigger
        public async Task<(bool IsSuccess, string Message, int? ClaimId)> SubmitClaimAsync(ClaimCreateDto dto)
        {
            try
            {
                //1. Lưu vào Oracle (Trigger TRG_CHECK_CLAIM_VALID) sẽ chạy ở đây
                int newClaimId = await _claimRepo.AddClaimAsync(dto);

                //2. làm giàu dữ liệu để chuẩn bị đồng bộ
                var synDto = await _claimRepo.GetClaimForSyncAsync(newClaimId);

                //3. Đồng bộ sang MongoDb (gửi gói tin sang Microservices Mongo)
                await SyncToMongoAsync(synDto);

                return (true, "Yêu cầu bồi thường đã được tạo và đồng bộ thành công .", newClaimId);
            }catch(OracleException ex) when (ex.Number >= 2000 && ex.Number <= 20999)
            {
                //Bắt các lỗi nghiệp vụ từ RAISE_APPLICATION_ERRO Trong Trigger
                return (false, $"Vi phạm nghiệp vụ bảo hiểm : {ex.Message}", null);
            }
        }

        private async Task SyncToMongoAsync(ClaimSyncDto dto)
        {
            var client = _httpClientFactory.CreateClient("MongoSyncClient");
            var response = await client.PostAsJsonAsync("http://api:8080/api/ClaimMongo/sync-from-oracle", dto);

            if (!response.IsSuccessStatusCode)
            {
                //Trong thực tế, có thể ghi log vào bảng "Sync_Retry" nếu có bảng, hoặc tọa thêm nếu cần quản lý
                Console.WriteLine("Cảnh báo : Đồng bộ sang MongoDb thất bại.");
            }
        }

        #endregion
    }
}
