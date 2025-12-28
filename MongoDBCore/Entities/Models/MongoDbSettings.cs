using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models
{
    public class MongoDbSettings
    {
        // Chuỗi kết nối MongoDB (ví dụ: mongodb://localhost:27017)
        public string ConnectionString { get; set; } = null!;

        // Tên cơ sở dữ liệu (Database Name)
        public string DatabaseName { get; set; } = null!;

        // Tên Collection (tập hợp các tài liệu) cho User
        public string CustomerCollectionName { get; set; } = null!;

        public string InsuranceTypeCollectionName { get; set; } = null!;

        public string AgentCollectionName { get; set; } = null!;
    }
}
