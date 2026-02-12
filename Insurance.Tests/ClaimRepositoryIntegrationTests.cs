using Dapper;
using FluentAssertions;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insurance.Tests
{
    public class ClaimRepositoryIntegrationTests
    {
        // Kết nối tới DB thật (Docker Oracle)
        private readonly string _connectionString = "User Id=system;Password=mypassword123;Data Source=localhost:1522/XEPDB1";


        [Fact(Skip = "Yêu cầu Oracle Docker chạy cục bộ")]
        //[Fact]
        public async Task GetTotalClaimedAmountByPolicyIdAsync_ShouldReturnCorrectSum_ExcludingCancelled()
        {
            //1. Arrange: Khoi tao Repo va chuan bi 2 Claim cho cung 1 Policy
            var repository = new ClaimRepository(_connectionString);
            int policyId = 28;

            //Tao 2 ban ghi
            // Tạo 2 bản ghi tạm thời
            int claimId1 = await CreateTestClaimDirectly(policyId, 1000000); // 1 triệu
            int claimId2 = await CreateTestClaimDirectly(policyId, 2000000); // 2 triệu

            try
            {
                //2. Act: Goi ham tinh tong tu Repository
                decimal total = await repository.GetTotalClaimedAmountByPolicyIdAsync(policyId);

                // 3. Assert: Kiem tra tong (Phai it nhat la 3 trieu tu 2 ban ghi moi tao)
                total.Should().BeGreaterThanOrEqualTo(3000000);

                // 4. Kiểm chứng logic loại bỏ hồ sơ CANCELLED
                // Thử hủy 1 hồ sơ
                await repository.CancelClaimAsync(claimId1, "Hủy để test tính tổng");

                decimal totalAfterCancel = await repository.GetTotalClaimedAmountByPolicyIdAsync(policyId);

                // Tổng mới phải giảm đi đúng 1 triệu của hồ sơ vừa hủy
                totalAfterCancel.Should().Be(total - 1000000);
            }
            finally
            {
                // Cleanup
                await DeleteTestClaim(claimId1);
                await DeleteTestClaim(claimId2);
            }
        }

        //[Fact]
        [Fact(Skip = "Yêu cầu Oracle Docker chạy cục bộ")]
        public async Task UpdateClaimStatusAsync_WhenValidApproved_ShouldReturnTrueAndUpdateDb()
        {
            // 1. Arrange: Khởi tạo Repository và dữ liệu mẫu
            var repository = new ClaimRepository(_connectionString);
            int testClaimId = await CreateTestClaimDirectly();

            try
            {
                // 2. Act: Gọi hàm nghiệp vụ xử lý bồi thường
                var result = await repository.UpdateClaimStatusAsync(
                    testClaimId,
                    "APPROVED",
                    1000000,
                    "Duyệt bồi thường từ Integration Test"
                );

                // 3. Assert: Kiểm tra giá trị trả về từ hàm C#
                result.Should().BeTrue();

                // 4. Verify: Truy vấn trực tiếp để xác nhận DB đã thay đổi
                using var conn = new OracleConnection(_connectionString);
                var dbClaim = await conn.QuerySingleOrDefaultAsync(
                    "SELECT STATUS, AMOUNT_APPROVED FROM INSURANCE_USER.DHN_CLAIM WHERE CLAIM_ID = :Id",
                    new { Id = testClaimId });

                // Lưu ý: Oracle thường trả về tên cột viết HOA, dùng Convert để an toàn kiểu dữ liệu
                string actualStatus = dbClaim.STATUS;
                decimal actualAmount = Convert.ToDecimal(dbClaim.AMOUNT_APPROVED);

                actualStatus.Should().Be("APPROVED");
                actualAmount.Should().Be(1000000);
            }
            finally
            {
                // Cleanup: Xóa dữ liệu test sau khi chạy xong để sạch DB
                await DeleteTestClaim(testClaimId);
            }
        }
        //[Fact]
        [Fact(Skip = "Yêu cầu Oracle Docker chạy cục bộ")]
        public async Task UpdateClaimStatusAsync_WhenClaimAlreadyProcessed_ShouldThrowException()
        {
            // 1. Arrange: Tạo một Claim và xử lý nó về trạng thái REJECTED trước
            var repository = new ClaimRepository(_connectionString);
            int testClaimId = await CreateTestClaimDirectly();

            try
            {
                await repository.UpdateClaimStatusAsync(testClaimId, "REJECTED", 0, "First Action");

                // 2. Act: Thử xử lý lại lần thứ 2 (đang là REJECTED không được duyệt tiếp)
                var act = async () => await repository.UpdateClaimStatusAsync(testClaimId, "APPROVED", 5000, "Second Action");

                // 3. Assert: Kiểm tra xem có bắt được Exception chứa thông báo từ Oracle Package không
                await act.Should().ThrowAsync<Exception>()
                    .WithMessage("*Loi: Yeu cau boi thuong nay da duoc xu ly truoc do!*");
            }
            finally
            {
                await DeleteTestClaim(testClaimId);
            }
        }

        // --- Helper Methods ---

        private async Task<int> CreateTestClaimDirectly()
        {
            using var conn = new OracleConnection(_connectionString);
            var p = new DynamicParameters();

            // ĐÂY LÀ DỮ LIỆU ĐẦU VÀO ĐỂ TẠO RA MỘT BẢN GHI THỬ NGHIỆM
            p.Add("v_policy_id", 28); // Claim phải thuộc về 1 Policy nào đó
            p.Add("v_amount", 2000000);
            p.Add("v_desc", "Data for testing");

            // ĐÂY LÀ CÁI CHÚNG TA CẦN LẤY VỀ ĐỂ TRUYỀN VÀO HÀM UPDATE
            p.Add("v_generated_claim_id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                @"INSERT INTO INSURANCE_USER.DHN_CLAIM (POLICY_ID, AMOUNT_CLAIMED, DESCRIPTION, STATUS) 
          VALUES (:v_policy_id, :v_amount, :v_desc, 'PENDING') 
          RETURNING CLAIM_ID INTO :v_generated_claim_id", p);

            return p.Get<int>("v_generated_claim_id");
        }

        private async Task<int> CreateTestClaimDirectly(int policyId = 28, decimal amount = 2000000)
        {
            using var conn = new OracleConnection(_connectionString);
            var p = new DynamicParameters();
            p.Add("v_policy_id", policyId);
            p.Add("v_amount", amount);
            p.Add("v_desc", "Data for testing aggregation");
            p.Add("v_generated_claim_id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                @"INSERT INTO INSURANCE_USER.DHN_CLAIM (POLICY_ID, AMOUNT_CLAIMED, DESCRIPTION, STATUS) 
          VALUES (:v_policy_id, :v_amount, :v_desc, 'PENDING') 
          RETURNING CLAIM_ID INTO :v_generated_claim_id", p);

            return p.Get<int>("v_generated_claim_id");
        }



        private async Task DeleteTestClaim(int claimId)
        {
            using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync(); // Đảm bảo connection đã mở

            // PHẢI xóa History trước
            await conn.ExecuteAsync(
                "DELETE FROM INSURANCE_USER.DHN_CLAIM_HISTORY WHERE CLAIM_ID = :Id",
                new { Id = claimId });

            // Sau đó mới xóa Claim
            await conn.ExecuteAsync(
                "DELETE FROM INSURANCE_USER.DHN_CLAIM WHERE CLAIM_ID = :Id",
                new { Id = claimId });
        }
    }
}