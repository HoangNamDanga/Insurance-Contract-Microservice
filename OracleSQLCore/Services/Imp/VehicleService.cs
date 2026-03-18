using MassTransit;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using Polly.Retry;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OracleSQLCore.Services.Imp
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly AsyncRetryPolicy _retryPolicy;

        public VehicleService(
            IVehicleRepository vehicleRepository,
            IPublishEndpoint publishEndpoint,
            AsyncRetryPolicy retryPolicy)
        {
            _vehicleRepository = vehicleRepository;
            _publishEndpoint = publishEndpoint;
            _retryPolicy = retryPolicy;
        }

        public async Task<int> CreateVehicleAsync(VehicleDto vehicleDto)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                // 1. Lưu vào Oracle (Nghiệp vụ ghi là ưu tiên số 1)
                int newVehicleId = await _vehicleRepository.CreateAsync(vehicleDto);

                // 2. Bắn event lên RabbitMQ để bên Mongo tự lo phần hiển thị và cache
                await _publishEndpoint.Publish(new VehicleCreatedEvent
                {
                    VehicleId = newVehicleId,
                    PolicyId = vehicleDto.PolicyId,
                    LicensePlate = vehicleDto.LicensePlate,
                    Brand = vehicleDto.Brand,
                    Model = vehicleDto.Model,
                    YearManufactured = vehicleDto.YearManufactured,
                    CreatedAt = DateTime.UtcNow,
                });

                return newVehicleId;
            });
        }

        public async Task<bool> UpdateVehicleAsync(VehicleDto vehicleDto)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                // 1. Cập nhật Oracle
                bool updated = await _vehicleRepository.UpdateAsync(vehicleDto);

                if (updated)
                {
                    // 2. Bắn Event Update để đồng bộ sang MongoDB
                    // Lưu ý: Bạn nên tạo thêm VehicleUpdatedEvent trong Shared.Contracts
                    await _publishEndpoint.Publish(new VehicleUpdatedEvent
                    {
                        VehicleId = vehicleDto.VehicleId,
                        PolicyId = vehicleDto.PolicyId,
                        LicensePlate = vehicleDto.LicensePlate,
                        Brand = vehicleDto.Brand,
                        Model = vehicleDto.Model,
                        YearManufactured = vehicleDto.YearManufactured,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                return updated;
            });
        }

        public async Task<bool> DeleteVehicleAsync(int vehicleId)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                // Lấy thông tin xe trước khi xóa để lấy PolicyId gửi đi
                var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null) return false;

                bool deleted = await _vehicleRepository.DeleteAsync(vehicleId);
                if (deleted)
                {
                    // 3. Bắn Event xóa để bên Mongo thực hiện $pull khỏi mảng
                    await _publishEndpoint.Publish(new VehicleDeletedEvent
                    {
                        VehicleId = vehicleId,
                        PolicyId = vehicle.PolicyId
                    });
                }

                return deleted;
            });
        }

        // Các hàm Get này giờ sẽ đọc trực tiếp từ Oracle nếu cần, 
        // nhưng thực tế Client nên gọi sang bên Mongo để lấy dữ liệu có cache.
        public async Task<VehicleDto> GetVehicleByIdAsync(int vehicleId)
        {
            return await _vehicleRepository.GetByIdAsync(vehicleId);
        }

        public async Task<IEnumerable<VehicleDto>> GetVehiclesByPolicyIdAsync(int policyId)
        {
            return await _vehicleRepository.GetByPolicyIdAsync(policyId);
        }
    }
}