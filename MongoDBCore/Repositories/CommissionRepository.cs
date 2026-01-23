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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MongoDBCore.Repositories
{
    public class CommissionRepository : ICommissionRepository
    {

        private readonly IMongoCollection<AgentCommissionMongo> _commisstionCollectionName;

        public CommissionRepository(IOptions<MongoDbSettings> options)
        {
            var settings = options.Value;
            var mongoClient = new MongoClient(settings.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);

            _commisstionCollectionName = mongoDatabase.GetCollection<AgentCommissionMongo>(settings.CommissionsCollectionName);
        }

        //Repo này dùng cho nghiệp vụ thanh toán tổng hợp rabbitMQ , và tính phí hoa hồng Commissition HTTP
        public async Task UpsertCommissionAsync(AgentCommissionMongo data)
        {
            // Sử dụng ReplaceOne với IsUpsert = true
            // Nếu tìm thấy PolicyId tương ứng thì ghi đè, nếu không thấy thì thêm mới
            await _commisstionCollectionName.ReplaceOneAsync(
                filter: x => x.PolicyId == data.PolicyId,
                replacement: data,
                options: new ReplaceOptions { IsUpsert = true }
            );
        }
    }
}
