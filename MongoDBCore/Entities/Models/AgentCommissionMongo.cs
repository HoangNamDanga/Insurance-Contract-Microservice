using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models
{
    public class AgentCommissionMongo
    {
        [BsonId] // Sử dụng PolicyId làm khóa chính để dễ truy vấn và tránh trùng
        public int PolicyId { get; set; }

        public string PolicyNumber { get; set; }
        public string CustomerName { get; set; }
        public int AgentId { get; set; }
        public string AgentName { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalPayment { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CommissionAmount { get; set; }

        public string Status { get; set; }
        public string SyncDate { get; set; }

        public DateTime LastUpdatedAt { get; set; } = DateTime.Now;
    }
}
