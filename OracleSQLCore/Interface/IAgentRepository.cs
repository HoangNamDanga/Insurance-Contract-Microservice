using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Interface
{
    public interface IAgentRepository
    {
        AgentDto Create(AgentDto agent);
        void Update(AgentDto agent);
        void Delete(AgentDto agent);
        AgentDto GetById(int id);
        List<AgentDto> GetAll();
    }
}
