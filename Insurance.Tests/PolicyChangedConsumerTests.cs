using MassTransit;
using MongoDBCore.Interfaces;
using MongoDBCore.Repositories.Consumer;
using MongoDBCore.Services;
using Shared.Contracts.Events;
using System.Net.WebSockets;

namespace Insurance.Tests
{
    public class PolicyChangedConsumerTests
    {
        [Fact]
        public async Task Consume_CancelAction_ShouldCallUpdateCancelStatus_And_RemoveCache()
        {
            // 1. Arrange
            var mocRepo = new Mock<IPolicyRepository>();
            var mockCache = new Mock<ICacheService>(); // TẠO MOCK CACHE TẠI ĐÂY

            // Truyền cả 2 mock vào consumer
            var consumer = new PolicyChangedConsumer(mocRepo.Object, mockCache.Object);
            var mockContext = new Mock<ConsumeContext<PolicyChangedEvent>>();

            var message = new PolicyChangedEvent
            {
                ActionType = "CANCEL",
                PolicyId = 101,
                LastNotes = "Test CI/CD"
            };

            mockContext.Setup(x => x.Message).Returns(message);

            // 2. Act
            await consumer.Consume(mockContext.Object);

            // 3. Assert
            // Kiểm tra DB được cập nhật
            mocRepo.Verify(x => x.UpdateCancelStatusAsync(101, "Test CI/CD"), Times.Once);

            // KIỂM TRA THÊM: Xem hàm xóa cache có được gọi với đúng Key không
            mockCache.Verify(x => x.RemoveAsync("policy:101"), Times.Once);
        }

        [Fact]
        public async Task Consume_RenewAction_ShouldCallUpdateRenewStatus_And_RemoveCache()
        {
            // Arrange
            var mocRepo = new Mock<IPolicyRepository>();
            var mockCache = new Mock<ICacheService>(); // TẠO MOCK CACHE TẠI ĐÂY

            var consumer = new PolicyChangedConsumer(mocRepo.Object, mockCache.Object);
            var mockContext = new Mock<ConsumeContext<PolicyChangedEvent>>();

            var message = new PolicyChangedEvent
            {
                ActionType = "RENEW",
                PolicyId = 102,
                LastNotes = "Test Renew CI/CD"
            };
            mockContext.Setup(x => x.Message).Returns(message);

            // Act
            await consumer.Consume(mockContext.Object);

            // Assert
            mocRepo.Verify(x => x.UpdateRenewStatusAsync(
                102,
                It.IsAny<DateTime>(),
                It.IsAny<decimal>(),
                "Test Renew CI/CD"
            ), Times.Once);

            // KIỂM TRA THÊM: Xóa cache khi Renew
            mockCache.Verify(x => x.RemoveAsync("policy:102"), Times.Once);
        }
    }
}
