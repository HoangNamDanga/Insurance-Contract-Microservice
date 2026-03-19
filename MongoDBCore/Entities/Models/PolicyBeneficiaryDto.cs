using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models
{
    public class PolicyBeneficiaryDto
    {
        [BsonId] // Không dùng BsonRepresentation(BsonType.ObjectId) ở đây
        public int BeneficiaryId { get; set; }
        public int PolicyId { get; set; }
        public string FullName { get; set; }
        public string Relationship { get; set; }
        public string Phone { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

    }
}
