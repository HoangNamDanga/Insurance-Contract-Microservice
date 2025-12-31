using MassTransit;
using MongoDBCore.Interfaces;
using MongoDBCore.Repositories.Consumer;
using Shared.Contracts.Events;
using System.Net.WebSockets;

namespace Insurance.Tests
{
    public class PolicyChangedConsumerTests
    {
        [Fact]
        public async Task Consume_CancelAction_ShouldCallUpdateCancelStatus()
        {
            // 1. Arrange: Chuẩn bị giả lập
            var mocRepo = new Mock<IPolicyRepository>();
            var consumer = new PolicyChangedConsumer(mocRepo.Object);
            var mockContext = new Mock<ConsumeContext<PolicyChangedEvent>>();

            var message = new PolicyChangedEvent
            {
                ActionType = "CANCEL",
                PolicyId = 101,
                LastNotes = "Test CI/CD"
            };

            mockContext.Setup(x => x.Message).Returns(message);

            // 2. Act: Chay thu ham Consume
            await consumer.Consume(mockContext.Object);

            //3. Assert: Kiem tra xem ham UpdateCanelStatus co duoc goi dung 1 lan
            mocRepo.Verify(x => x.UpdateCancelStatusAsync(101, "Test CI/CD"), Times.Once);
        }   
    }
}
