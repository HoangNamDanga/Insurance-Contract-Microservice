using Dapper;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Repositories;
using Shared.Contracts.Events;
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
                // 1. Chạy Procedure hủy trong Oracle
                bool isCancelled = await _claimRepo.CancelClaimAsync(claimId, reason);

                if (isCancelled)
                {
                    // 2. Lấy PolicyId thuộc về Claim này (Bạn cần viết thêm hàm này trong Repo)
                    int policyId = await _claimRepo.GetPolicyIdByClaimId(claimId);

                    // 3. Làm giàu lại toàn bộ Hợp đồng (Mảng Claims lúc này sẽ cập nhật trạng thái mới)
                    var enrichedPolicy = await _claimRepo.EnrichPolicyData(policyId, "UPDATE");

                    // 4. Đồng bộ Snapshot mới nhất sang Mongo
                    await SyncToMongoAsync(enrichedPolicy);

                    return (true, "Hủy yêu cầu bồi thường và cập nhật báo cáo thành công.");
                }

                return (false, "Không thể hủy hồ sơ. Có thể hồ sơ không ở trạng thái PENDING.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        public async Task<decimal> GetTotalClaimedAmountByPolicyIdAsync(int policyId)
        {
            try
            {
                // Gọi Repo để lấy con số tổng từ Oracle
                var total = await _claimRepo.GetTotalClaimedAmountByPolicyIdAsync(policyId);

                return total;
            }
            catch (Exception ex)
            {
                // Log lỗi tại đây (ví dụ: _logger.LogError(ex, "..."))
                // Trả về 0 để tránh làm lỗi giao diện người dùng (UI Dashboard)
                return 0;
            }
        }


        //Thủ tục Duyệt hoặc Từ chối bồi thường
        public async Task<(bool IsSuccess, string Message)> ProcessClaimStatusAsync(int claimId, string status, decimal? amountApproved, string note)
        {
            try
            {
                // 1. Cập nhật Oracle
                bool isUpdated = await _claimRepo.UpdateClaimStatusAsync(claimId, status, amountApproved, note);

                if (isUpdated)
                {
                    // 2. Lấy PolicyId để làm giàu dữ liệu
                    int policyId = await _claimRepo.GetPolicyIdByClaimId(claimId);

                    // 3. Lấy Snapshot mới (Hàm Enrich sẽ sum lại số tiền bồi thường mới nhất)
                    var enrichedPolicy = await _claimRepo.EnrichPolicyData(policyId, "UPDATE");

                    // 4. Đồng bộ sang MongoDB
                    await SyncToMongoAsync(enrichedPolicy);

                    return (true, "Cập nhật trạng thái và đồng bộ báo cáo thành công.");
                }
                return (false, "Không thể cập nhật trạng thái hồ sơ.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        #region Nghiệp vụ Quản lý Bồi thường (Claims Management) Validate trigger
        public async Task<(bool IsSuccess, string Message, int? ClaimId)> SubmitClaimAsync(ClaimCreateDto dto)
        {
            try
            {
                // Repo trả về nguyên Snapshot (Object)
                var enrichedPolicy = await _claimRepo.AddClaimAsync(dto);

                // Đồng bộ Snapshot sang Mongo
                await SyncToMongoAsync(enrichedPolicy);

                // LẤY ID: Trong danh sách Claims của Snapshot, cái cuối cùng chính là cái vừa tạo
                // Hoặc nếu trong Event bạn có trường ClaimId riêng thì dùng trường đó
                int? lastClaimId = enrichedPolicy.Claims.OrderByDescending(c => c.ClaimId).FirstOrDefault()?.ClaimId;

                return (true, "Bồi thường thành công.", lastClaimId);
            }
            catch (OracleException ex) when (ex.Number >= 20000 && ex.Number <= 20999)
            {
                return (false, $"Lỗi nghiệp vụ: {ex.Message}", null);
            }
        }



        // 1. Đổi tham số thành PolicyCreatedEvent
        private async Task SyncToMongoAsync(PolicyCreatedEvent fullPolicyData)
        {
            try
            {
                // 1. Lấy Client đã có sẵn Retry & Circuit Breaker từ Polly
                var client = _httpClientFactory.CreateClient("MongoSyncClient");


                // Đường dẫn đầy đủ sẽ là: http://api:8080/api/ClaimMongo/sync-from-oraclee
                var response = await client.PostAsJsonAsync("api/ClaimMongo/sync-from-oraclee", fullPolicyData);

                // 3. Xử lý kết quả
                if (!response.IsSuccessStatusCode)
                {
                    // Đọc nội dung lỗi từ phía MongoDB để dễ debug
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Sync Error] {response.StatusCode}: {errorContent}");
                }
                else
                {
                    Console.WriteLine($"[Sync Success] Đã đồng bộ Policy {fullPolicyData.PolicyId} sang MongoDB.");
                }
            }
            catch (Exception ex)
            {
                // Nếu rơi vào đây nghĩa là Polly đã Retry hết số lần cho phép 
                // hoặc Circuit Breaker đang ở trạng thái Open (Ngắt mạch)
                Console.WriteLine($"[System Error] Không thể đồng bộ sau khi đã Retry: {ex.Message}");
            }
        }

        #endregion
    }
}
