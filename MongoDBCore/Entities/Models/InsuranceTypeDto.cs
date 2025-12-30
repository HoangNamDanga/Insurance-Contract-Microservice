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
        //// Đảm bảo khi lưu xuống Mongo, tên cột sẽ là _id nhưng map vào code vẫn là InsTypeId
        public int InsTypeId { get; set; }
        public string? TypeName { get; set; }
        public string? Description { get; set; }
    }
}
