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

    // Test
    //cần chạy để đảm bảo SQL và Trigger hoạt động đúng trước khi push.
    //hàm Integration Test (có Skip): Nó LIỆT KÊ vào danh sách nhưng KHÔNG CHẠY nội dung bên trong
    // Mục đích : Kiểm tra code có thực sự ghi được vào Oracle không.
    // Dùng Test Explorer để xác nhận dữ liệu đã vào DB
    // Quá trình này test bằng thủ công bằng tay, vì trên git không có môi trường database, skip đẻ bỏ qua hàm này vì nó đã đc chạy dưới local của mình rồi
    //sự phối hợp giữa Local Test và CI/CD tự động
    // Chú ý : [Fact(Skip)] nghĩa là bỏ qua sau khi push lên git CI/CD tự động để tránh lỗi. Để Fact khi muốn test dưới local, sau đó lại [Fact(Skip)] khi đẩy lên git khi đã pass

    public class ClaimRepositoryTests
    {
        // Chú ý: Dùng Connection String tới Docker Oracle đang chạy trên máy bạn (cổng 1522)
        private readonly string _connectionString = "User Id=system;Password=mypassword123;Data Source=localhost:1522/XEPDB1";

        [Fact(Skip = "Confirmed locally. Skip for CI/CD pass.")]
        //[Fact]
        public async Task AddClaimAsync_ShouldInsertData_AndReturnValidGeneratedId()
        {
            // 1. Arrange: Khởi tạo Repository và dữ liệu mẫu
            var repository = new ClaimRepository(_connectionString);
            var testDto = new ClaimCreateDto
            {
                PolicyId = 28, // Đảm bảo ID này tồn tại trong bảng Policy nếu có FK
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

            // Sử dụng kiểu IDictionary để tránh lỗi Dynamic null reference
            var insertedClaim = await connection.QuerySingleOrDefaultAsync(
                "SELECT DESCRIPTION, STATUS FROM INSURANCE_USER.DHN_CLAIM WHERE CLAIM_ID = :Id",
                new { Id = newId }) as IDictionary<string, object>;

            // Kiểm tra xem có tìm thấy bản ghi không
            insertedClaim.Should().NotBeNull($"Không tìm thấy bản ghi với ID {newId} trong bảng DHN_CLAIM");

            // Oracle thường trả về tên cột viết HOA TOÀN BỘ
            insertedClaim["DESCRIPTION"].ToString().Should().Be(testDto.Description);
            insertedClaim["STATUS"].ToString().Should().Be("Pending");
        }
    }
}
