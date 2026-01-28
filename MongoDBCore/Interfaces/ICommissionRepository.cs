using MongoDBCore.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Interfaces
{
    public interface ICommissionRepository
    {
        Task UpsertCommissionAsync(AgentCommissionMongo data);

        Task UpsertCommissionAsync(CommissionMongo data);
    }
}
