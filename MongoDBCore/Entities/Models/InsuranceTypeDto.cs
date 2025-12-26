using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models
{
    public class InsuranceTypeDto
    {
        [BsonId] // Đánh dấu đây là khóa chính cho Mongo
        public int InsTypeId { get; set; }
        public string? TypeName { get; set; }
        public string? Description { get; set; }
    }
}
