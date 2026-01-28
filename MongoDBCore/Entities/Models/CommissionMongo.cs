using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models
{
    public class CommissionMongo
    {
        [BsonId]
        // Dùng PaymentId làm khóa chính vì mỗi giao dịch thanh toán là duy nhất
        public int PaymentId { get; set; }

        public int PolicyId { get; set; }
        public string? PolicyNumber { get; set; }
        public string? CustomerName { get; set; }

        public int AgentId { get; set; }
        public string? AgentName { get; set; }
        public string? NewAgentLevel { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalPayment { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CommissionAmount { get; set; }

        public string? Status { get; set; } // Ví dụ: "SUCCESS"
        public DateTime ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
