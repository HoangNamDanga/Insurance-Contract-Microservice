using MongoDBCore.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Interfaces
{
    public interface IAgentRepository
    {
        Task<List<AgentDto>> GetAllAsync();
        Task<AgentDto> GetByIdAsync(int id);
        Task UpsertAsync(AgentDto agentDto);
        Task DeleteAsync(int id);

    }
}
