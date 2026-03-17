using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models
{
    public class PaymentDto
    {
        [BsonId] // Đánh dấu đây là khóa chính trong MongoDB
        public decimal PaymentId { get; set; }

        public decimal PolicyId { get; set; }

        public DateTime? PaymentDate { get; set; }

        public string? PaymentPeriod { get; set; }

        public string? TransactionId { get; set; }

        // Cấu hình để MongoDB lưu decimal chính xác dưới dạng Decimal128
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }

        public string? Method { get; set; }

        public string? Status { get; set; }

        public DateTime CreateAt { get; set; }
    }
}
