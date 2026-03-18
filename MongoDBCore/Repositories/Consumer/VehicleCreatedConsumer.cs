using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDBCore.Interfaces;
using Shared.Contracts.Events;

namespace MongoSync.Service.Consumers
{
    public class VehicleCreatedConsumer : IConsumer<VehicleCreatedEvent>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ILogger<VehicleCreatedConsumer> _logger;

        public VehicleCreatedConsumer(IVehicleRepository vehicleRepository, ILogger<VehicleCreatedConsumer> logger)
        {
            _vehicleRepository = vehicleRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<VehicleCreatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("--> [MongoSync] Nhận CreatedEvent: Xe {VehicleId}", message.VehicleId);

            // Gọi Repository để Upsert và tự động xóa Cache
            await _vehicleRepository.UpsertVehicleAsync(message.PolicyId, message);
        }
    }
}