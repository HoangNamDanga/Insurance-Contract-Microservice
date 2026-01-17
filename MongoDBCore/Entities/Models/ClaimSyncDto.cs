using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Entities.Models
{
    public class ClaimSyncDto
    {
        [BsonId]
        // Sử dụng ClaimId từ Oracle làm ID bên Mongo để dễ đối chiếu
        public int ClaimId { get; set; }

        public int PolicyId { get; set; }

        public string PolicyNumber { get; set; }

        public string CustomerName { get; set; }

        public DateTime ClaimDate { get; set; }

        public decimal AmountClaimed { get; set; }

        public string Status { get; set; }

        public string Description { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime SyncDate { get; set; }
    }
}
