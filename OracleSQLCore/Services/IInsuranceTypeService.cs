using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services
{
    public interface IInsuranceTypeService
    {
        InsuranceTypeDto Create(InsuranceTypeDto dto);
        void Update(InsuranceTypeDto dto);
        void Delete(int id);
        InsuranceTypeDto GetById(int id);
        List<InsuranceTypeDto> GetAll();
    }
}
