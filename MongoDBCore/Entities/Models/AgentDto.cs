using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models
{
    [BsonIgnoreExtraElements]
    public class AgentDto
    {
        [BsonId]
        public int AgentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int? BranchId { get; set; }

        // Thời gian tạo (sử dụng múi giờ UTC)
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? HireDate { get; set; }
    }
}
