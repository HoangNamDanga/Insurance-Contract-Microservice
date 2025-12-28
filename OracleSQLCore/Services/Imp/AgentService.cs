using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services.Imp
{
    public class AgentService : IAgentService
    {
        private readonly IAgentRepository _repository;

        public AgentService(IAgentRepository repository)
        {
            _repository = repository;
        }

        public AgentDto Create(AgentDto agent)
        {
            return _repository.Create(agent);
        }

        public void Delete(AgentDto agent)
        {
            _repository.Delete(agent);
        }

        public List<AgentDto> GetAll()
        {
            return _repository.GetAll();
        }

        public AgentDto GetById(int id)
        {
            return _repository.GetById(id);
        }

        public void Update(AgentDto agent)
        {
            _repository.Update(agent);
        }
    }
}
