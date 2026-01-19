using Dapper;
using FluentAssertions;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insurance.Tests
{
    //Ci/CD kiểu Integration Test : Loại viết test cho lớp logic Database
    public class ClaimRepositoryTests
    {
        // Chú ý: Dùng Connection String tới Docker Oracle đang chạy trên máy bạn (cổng 1522)
        private readonly string _connectionString = "User Id=system;Password=mypassword123;Data Source=localhost:1522/XEPDB1";

        [Fact]
        public async Task AddClaimAsync_ShouldInsertData_AndReturnValidGeneratedId()
        {
            // 1. Arrange: Khởi tạo Repository và dữ liệu mẫu
            var repository = new ClaimRepository(_connectionString);
            var testDto = new ClaimCreateDto
            {
                PolicyId = 1, // Đảm bảo ID này tồn tại trong bảng Policy nếu có FK
                ClaimDate = DateTime.Now,
                AmountClaimed = 1500000,
                Description = "Test bồi thường từ Unit Test"
            };

            // 2. Act: Gọi hàm thực hiện insert
            var newId = await repository.AddClaimAsync(testDto);

            // 3. Assert: Kiểm tra ID trả về
            newId.Should().BeGreaterThan(0, "Vì Trigger trong Oracle phải tự sinh ID dương");

            // 4. Verify: Truy vấn trực tiếp vào DB để chắc chắn dữ liệu đã nằm trong bảng
            using var connection = new OracleConnection(_connectionString);
            var insertedClaim = await connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM INSURANCE_USER.DHN_CLAIM WHERE CLAIM_ID = :Id",
                new { Id = newId });

            insertedClaim.Should().NotBeNull();
            ((string)insertedClaim.DESCRIPTION).Should().Be(testDto.Description);
            ((string)insertedClaim.STATUS).Should().Be("Pending");
        }
    }
}
