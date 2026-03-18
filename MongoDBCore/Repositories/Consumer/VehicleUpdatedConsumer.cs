using MassTransit;
using MongoDBCore.Interfaces;
using Shared.Contracts.Events;

namespace MongoSync.Service.Consumers
{
    public class VehicleUpdatedConsumer : IConsumer<VehicleUpdatedEvent>
    {
        private readonly IVehicleRepository _vehicleRepository;

        public VehicleUpdatedConsumer(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        public async Task Consume(ConsumeContext<VehicleUpdatedEvent> context)
        {
            var message = context.Message;

            // Map từ UpdatedEvent sang CreatedEvent để dùng chung hàm Upsert (tiết kiệm code)
            var data = new VehicleCreatedEvent
            {
                VehicleId = message.VehicleId,
                PolicyId = message.PolicyId,
                LicensePlate = message.LicensePlate,
                Brand = message.Brand,
                Model = message.Model,
                YearManufactured = message.YearManufactured,
                CreatedAt = message.UpdatedAt // Lấy mốc thời gian mới nhất
            };

            await _vehicleRepository.UpsertVehicleAsync(message.PolicyId, data);
        }
    }
}