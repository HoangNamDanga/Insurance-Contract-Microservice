using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models
{
    public class Customer
    {
        // Thuộc tính Id: Khóa chính của MongoDB, kiểu ObjectId ánh xạ thành string
        [BsonId]
        public string? Id { get; set; }

        // Thuộc tính Tên đầy đủ (sử dụng BsonElement nếu tên cột khác tên thuộc tính)
        [BsonElement("Name")]
        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        // Thời gian tạo (sử dụng múi giờ UTC)
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }
    }
}
