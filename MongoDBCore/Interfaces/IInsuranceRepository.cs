using MongoDBCore.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Interfaces
{
    public interface IInsuranceRepository
    {
        Task<List<InsuranceTypeDto>> GetAllAsync();
        Task<InsuranceTypeDto> GetByIdAsync(int id);
        Task UpsertAsync(InsuranceTypeDto dto); // Thêm hàm này
        Task DeleteAsync(int id);               // Thêm hàm này
    }
}
