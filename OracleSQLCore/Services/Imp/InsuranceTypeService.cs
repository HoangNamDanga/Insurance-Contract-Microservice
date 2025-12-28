using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services.Imp
{
    public class InsuranceTypeService : IInsuranceTypeService
    {
        private readonly IInsuranceTypeRepository _repository;

        public InsuranceTypeService(IInsuranceTypeRepository repository)
        {
            _repository = repository;
        }

        public InsuranceTypeDto Create(InsuranceTypeDto dto)
        {
            return _repository.Create(dto);
        }

        public void Delete(int id)
        {
            _repository.Delete(id);
        }

        public List<InsuranceTypeDto> GetAll()
        {
            return _repository.GetAll();
        }

        public InsuranceTypeDto GetById(int id)
        {
            return _repository.GetById(id);
        }

        public void Update(InsuranceTypeDto dto)
        {
            _repository.Update(dto);
        }
    }
}
