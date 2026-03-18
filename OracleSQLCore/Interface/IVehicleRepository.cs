using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Interface
{
    public interface IVehicleRepository
    {
        Task<int> CreateAsync(VehicleDto vehicle);
        Task<VehicleDto> GetByIdAsync(int vehicleId);
        Task<IEnumerable<VehicleDto>>GetByPolicyIdAsync(int policyId);
        Task<bool> UpdateAsync(VehicleDto dto);
        Task<bool> DeleteAsync(int vehicleId);
    }
}
