using MassTransit;
using MongoDBCore.Interfaces;
using Shared.Contracts.Events;

namespace MongoSync.Service.Consumers
{
    public class VehicleDeletedConsumer : IConsumer<VehicleDeletedEvent>
    {
        private readonly IVehicleRepository _vehicleRepository;

        public VehicleDeletedConsumer(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        public async Task Consume(ConsumeContext<VehicleDeletedEvent> context)
        {
            var message = context.Message;

            // Thực hiện xóa Document trong Mongo và xóa Cache tương ứng
            await _vehicleRepository.RemoveVehicleAsync(message.PolicyId, message.VehicleId);
        }
    }
}