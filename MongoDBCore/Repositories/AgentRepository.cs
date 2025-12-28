using MassTransit.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories
{
    public class AgentRepository : IAgentRepository
    {
        private readonly IMongoCollection<AgentDto> _agentsCollection;

        public AgentRepository(IOptions<MongoDbSettings> options)
        {
            var settings = options.Value;
            var mongoClient = new MongoClient(settings.ConnectionString);
            
            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);

            _agentsCollection = mongoDatabase.GetCollection<AgentDto>(settings.AgentCollectionName);
        }

        public async Task DeleteAsync(int id)
        {
            var filter = Builders<AgentDto>.Filter.Eq(x => x.AgentId, id);
            await _agentsCollection.DeleteOneAsync(filter);
        }

        public async Task<List<AgentDto>> GetAllAsync()
        {
            return await _agentsCollection.Find(_ => true).ToListAsync();
        }

        public async Task<AgentDto> GetByIdAsync(int id)
        {
            return await _agentsCollection.Find(x => x.AgentId == id).FirstOrDefaultAsync();
        }

        public async Task UpsertAsync(AgentDto agentDto)
        {
            var filter = Builders<AgentDto>.Filter.Eq(x => x.AgentId, agentDto.AgentId);
            // IsUpsert = true: Tự động tạo Collection/Document nếu chưa tồn tại
            await _agentsCollection.ReplaceOneAsync(filter, agentDto, new ReplaceOptions { IsUpsert = true });
        }
    }
}
