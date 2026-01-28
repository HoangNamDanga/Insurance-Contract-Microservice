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
    //Class này là HTTP cũng đồng bộ vào, và RabbitMQ cũng sẽ gọi qua
    //Nghiệp vụ tính phí hoa hồng đại lý và nghiệp vụ tổng hợp hoa hồng dùng chung repo này
    public class CommissionRepository : ICommissionRepository
    {

        private readonly IMongoCollection<AgentCommissionMongo> _commisstionCollectionName;
        private readonly IMongoCollection<CommissionMongo> _commisstionPaymentCollectionName;
        public CommissionRepository(IOptions<MongoDbSettings> options)
        {
            var settings = options.Value;
            var mongoClient = new MongoClient(settings.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);

            _commisstionCollectionName = mongoDatabase.GetCollection<AgentCommissionMongo>(settings.CommissionsCollectionName);
            _commisstionPaymentCollectionName = mongoDatabase.GetCollection<CommissionMongo>(settings.PaymentsCollectionName);
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

        // dùng cho consumer
        public async Task UpsertCommissionAsync(CommissionMongo data)
        {
            await _commisstionPaymentCollectionName.ReplaceOneAsync(
            filter: x => x.PaymentId == data.PaymentId,
            replacement: data,
            options: new ReplaceOptions { IsUpsert = true }
            );
        }
    }
}
