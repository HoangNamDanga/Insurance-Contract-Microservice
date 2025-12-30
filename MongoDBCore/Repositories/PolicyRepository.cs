using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Repositories
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly IMongoCollection<PolicyDto> _policiesCollection;

        public PolicyRepository(IOptions<MongoDbSettings> options)
        {
            var settings = options.Value;
            var mongoClient = new MongoClient(settings.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);
            _policiesCollection = mongoDatabase.GetCollection<PolicyDto>(settings.PolicyCollectionName);
        }
        public async Task DeleteAsync(int id)
        {
            var filter = Builders<PolicyDto>.Filter.Eq(x => x.PolicyId, id);
            await _policiesCollection.DeleteOneAsync(filter);
        }

        public async Task<List<PolicyDto>> GetAllAsync()
        {
            return await _policiesCollection.Find(_ => true).ToListAsync();
        }

        public async Task<PolicyDto> GetByIdAsync(int id)
        {
            return await _policiesCollection.Find(x => x.PolicyId == id).FirstOrDefaultAsync();
        }

        public async Task UpsertAsync(PolicyDto policytDto)
        {
            var filter = Builders<PolicyDto>.Filter.Eq(x => x.PolicyId , policytDto.PolicyId);
            await _policiesCollection.ReplaceOneAsync(filter, policytDto, new ReplaceOptions {  IsUpsert = true });
        }


        //Gia han hop dong va huy hop dong
        public async Task UpdateCancelStatusAsync(int id, string notes)
        {
            var filter = Builders<PolicyDto>.Filter.Eq(x => x.PolicyId, id);
            var update = Builders<PolicyDto>.Update
                .Set(x => x.Status, "CANCELLED");

            await _policiesCollection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateRenewStatusAsync(int id, DateTime newEndDate, decimal totalPremium, string notes)
        {
            var filter = Builders<PolicyDto>.Filter.Eq(x => x.PolicyId, id);
            var update = Builders<PolicyDto>.Update
                .Set(x => x.Status, "RENEWED")
                .Set(x => x.EndDate, newEndDate)
                .Set(x => x.PremiumAmount, totalPremium);
            await _policiesCollection.UpdateOneAsync(filter, update);
        }
    }
}
