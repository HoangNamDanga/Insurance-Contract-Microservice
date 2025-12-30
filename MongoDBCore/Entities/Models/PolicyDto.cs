using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models
{
    public class PolicyDto
    {
        [BsonId] // Sử dụng ID từ Oracle làm khóa chính luôn
        public int PolicyId { get; set; }

        public string PolicyNumber { get; set; }

        // Các trường ID (vẫn giữ để đối soát nếu cần)
        public int CustomerId { get; set; }
        public int AgentId { get; set; }
        public int InsTypeId { get; set; }

        // CÁC TRƯỜNG "HỨNG" TỪ EVENT (Quan trọng để hiển thị)
        public string CustomerName { get; set; }
        public string AgentName { get; set; }
        public string InsTypeName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PremiumAmount { get; set; }
        public string Status { get; set; }
    }
}
