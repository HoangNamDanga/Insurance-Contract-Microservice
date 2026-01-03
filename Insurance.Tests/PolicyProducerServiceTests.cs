using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Repositories;
using OracleSQLCore.Services;
using OracleSQLCore.Services.Imp;
using Polly;
using Polly.Retry;
using Shared.Contracts.Events;
namespace Insurance.Tests
{

    public class PolicyProducerServiceTests
    {
        private readonly Mock<IPolicyRepository> _mocRepo;
        private readonly Mock<IPublishEndpoint> _mocBus;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly PolicyService _service;

        public PolicyProducerServiceTests()
        {
            _mocRepo = new Mock<IPolicyRepository>();
            _mocBus = new Mock<IPublishEndpoint>();

            // SỬA LỖI ÉP KIỂU: Tạo một AsyncRetryPolicy thực sự nhưng không retry
            _retryPolicy = Policy.Handle<Exception>().RetryAsync(0);

            _service = new PolicyService(
                _mocRepo.Object,
                _mocBus.Object,
                _retryPolicy
            );
        }


        //Đảm bảo khi Database (Oracle) báo đã gia hạn thành công, thì bắt buộc một tin nhắn thông báo (Event) phải được gửi đi.
        [Fact]
        public async Task RenewAsync_WhenSuccess_ShouldPublishEvent()
        {
            // 1. Arrange
            var request = new RenewPolicyDto { PolicyId = 101 };
            var resultEvent = new PolicyChangedEvent { PolicyId = 101, ActionType = "RENEW" };

            _mocRepo.Setup(x => x.RenewAsync(request)).ReturnsAsync(resultEvent);

            // 2. Act
            await _service.RenewAsync(request);

            // 3. Assert: Kiểm tra xem Publish có được gọi bên trong RetryPolicy hay không
            _mocBus.Verify(x => x.Publish(
                It.Is<PolicyChangedEvent>(m => m.PolicyId == 101 && m.ActionType == "RENEW"),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        //Kiểm tra luồng chạy thành công của nghiệp vụ Hủy.

        [Fact]
        public async Task CancelAsync_WhenSuccess_ShouldPublishEvent()
        {
            // 1. Arrange
            var request = new CancelPolicyDto { PolicyId = 202 };
            var resultEvent = new PolicyChangedEvent { PolicyId = 202, ActionType = "CANCEL" };

            _mocRepo.Setup(x => x.CancelAsync(request)).ReturnsAsync(resultEvent);

            // 2. Act
            await _service.CancelAsync(request);

            // 3. Assert
            _mocBus.Verify(x => x.Publish(
                It.Is<PolicyChangedEvent>(m => m.PolicyId == 202 && m.ActionType == "CANCEL"),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }



        //Đảm bảo hệ thống không báo linh tinh khi gặp lỗi
        //Tính chính xác
        //Kiểm tra tính an toàn khi dữ liệu lỗi (Kịch bản tiêu cực).
        [Fact]
        public async Task RenewAsync_WhenRepositoryReturnsNull_ShouldNotPublishEvent()
        {
            // Test trường hợp lỗi ở DB hoặc không tìm thấy Policy
            // 1. Arrange
            var request = new RenewPolicyDto { PolicyId = 999 };
            _mocRepo.Setup(x => x.RenewAsync(request)).ReturnsAsync((PolicyChangedEvent)null);

            // 2. Act
            await _service.RenewAsync(request);

            // 3. Assert: Không được phép gọi Publish nếu DB trả về null
            _mocBus.Verify(x => x.Publish(
                It.IsAny<PolicyChangedEvent>(),
                It.IsAny<CancellationToken>()
            ), Times.Never);
        }
    }
}

