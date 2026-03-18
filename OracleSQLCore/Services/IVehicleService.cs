using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services
{
    public interface IVehicleService
    {
        Task<int> CreateVehicleAsync(VehicleDto vehicleDto);
        Task<bool> UpdateVehicleAsync(VehicleDto vehicleDto);
        Task<bool> DeleteVehicleAsync(int vehicleId);
        Task<VehicleDto> GetVehicleByIdAsync(int vehicleId);
        Task<IEnumerable<VehicleDto>> GetVehiclesByPolicyIdAsync(int policyId);
    }
}
